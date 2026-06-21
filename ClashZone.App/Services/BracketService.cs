using ClashZone.App.Domains;
using ClashZone.App.Domains.Riot;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    public class BracketService : IBracketService
    {
        private const int MaxConcurrentEmailSends = 4;

        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly ICoinWalletService _coinWalletService;
        private readonly IMatchesRepository _matchesRepository;
        private readonly IPlayerStatsRepository _playerStatsRepository;
        private readonly IRiotMatchService _riotMatchService;
        public BracketService(
            ITournamentsRepository tournamentsRepository,
            IUserRepository userRepository,
            IEmailService emailService,
            ICoinWalletService coinWalletService,
            IMatchesRepository matchesRepository,
            IPlayerStatsRepository playerStatsRepository,
            IRiotMatchService riotMatchService)
        {
            _tournamentsRepository = tournamentsRepository;
            _userRepository = userRepository;
            _emailService = emailService;
            _coinWalletService = coinWalletService;
            _matchesRepository = matchesRepository;
            _playerStatsRepository = playerStatsRepository;
            _riotMatchService = riotMatchService;
        }

        public async Task<BracketViewModel?> GetBracketAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var (teamNames, idToName, nameToTeam) = await ResolveTeamNamesAsync(teams);
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfoDomain>>()
                };
            }
            var skeleton = GenerateBracketSkeleton(teamNames);
            var bracket = await BuildBracketWithDbAsync(tournamentId, skeleton, idToName);
            var annotated = AnnotateRounds(bracket);
            var existingMatches = await _matchesRepository.GetMatchesByTournamentAsync(tournamentId);
            if ((existingMatches == null || !existingMatches.Any())
                && await _tournamentsRepository.TryMarkInitialMatchRemindersSentAsync(tournamentId))
            {
                await NotifyInitialMatchesAsync(tournament, nameToTeam, annotated);
            }
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotated
            };
        }

        private async Task<(List<string> orderedNames, Dictionary<int, string> idToName, Dictionary<string, TeamDomain> nameToTeam)> ResolveTeamNamesAsync(List<TeamDomain> teams)
        {
            var orderedNames = new List<string>();
            var idToName = new Dictionary<int, string>();
            var nameToTeam = new Dictionary<string, TeamDomain>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }
                orderedNames.Add(name);
                idToName[team.Id] = name;
                if (!nameToTeam.ContainsKey(name))
                {
                    nameToTeam[name] = team;
                }
            }
            return (orderedNames, idToName, nameToTeam);
        }

        public async Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var (teamNames, _, _) = await ResolveTeamNamesAsync(teams);
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfoDomain>>()
                };
            }
            var rounds = GenerateBracketWithResults(teamNames, tournament.GameTitle);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        public async Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var (teamNames, _, nameToTeam) = await ResolveTeamNamesAsync(teams);
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfoDomain>>()
                };
            }
            var rounds = GenerateBracketWithResults(teamNames, tournament.GameTitle);
            await SaveBracketStatsAsync(rounds, tournament, nameToTeam);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        public async Task<BracketViewModel?> SimulateMatchAsync(int tournamentId, int roundNumber, int matchNumber, bool isAdmin = false)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var (teamNames, idToName, nameToTeam) = await ResolveTeamNamesAsync(teams);
            if (teamNames.Count < 2)
            {
                return null;
            }
            var skeleton = GenerateBracketSkeleton(teamNames);
            var bracket = await BuildBracketWithDbAsync(tournamentId, skeleton, idToName);
            var annotated = AnnotateRounds(bracket);

            int rIdx = roundNumber - 1;
            if (rIdx < 0 || rIdx >= annotated.Count)
            {
                return null;
            }
            bool isFinalRound = rIdx == annotated.Count - 1;
            int mIdx = matchNumber - 1;
            if (mIdx < 0 || mIdx >= annotated[rIdx].Count)
            {
                return null;
            }
            var matchInfo = annotated[rIdx][mIdx];
            if (string.IsNullOrEmpty(matchInfo.Team1Name) || string.IsNullOrEmpty(matchInfo.Team2Name))
            {
                return null;
            }
            if (matchInfo.Team1Score.HasValue || matchInfo.Team2Score.HasValue)
            {
                return null;
            }
           
            TeamDomain? team1 = nameToTeam.TryGetValue(matchInfo.Team1Name, out var t1) ? t1 : null;
            TeamDomain? team2 = nameToTeam.TryGetValue(matchInfo.Team2Name, out var t2) ? t2 : null;

            MatchDomain matchEntity;
            if (GameCatalog.IsLol(tournament.GameTitle))
            {
                var team1Members = await GetAllMemberIdsAsync(team1);
                var team2Members = await GetAllMemberIdsAsync(team2);

                var outcome = await _riotMatchService.ResolveMatchAsync(team1Members, team2Members, forceDevMode: isAdmin);
                if (outcome == null)
                {
                    return null;
                }
                matchEntity = new MatchDomain
                {
                    TournamentId = tournamentId,
                    Team1Id = team1?.Id,
                    Team2Id = team2?.Id,
                    Team1Score = outcome.Team1Score,
                    Team2Score = outcome.Team2Score,
                    PlayedAt = DateTime.UtcNow
                };
                await _matchesRepository.AddMatchAsync(matchEntity);
                await PersistRiotPlayerStatsAsync(matchEntity, outcome, tournament.GameTitle);
            }
            else
            {
                var (score1, score2) = GenerateMatchScores(tournament.GameTitle, new Random());
                matchEntity = new MatchDomain
                {
                    TournamentId = tournamentId,
                    Team1Id = team1?.Id,
                    Team2Id = team2?.Id,
                    Team1Score = score1,
                    Team2Score = score2,
                    PlayedAt = DateTime.UtcNow
                };
                await _matchesRepository.AddMatchAsync(matchEntity);
                await GenerateStatsForMatchAsync(matchEntity, team1, team2, tournament.GameTitle);
            }
            bracket = await BuildBracketWithDbAsync(tournamentId, skeleton, idToName);
            annotated = AnnotateRounds(bracket);
            if (isFinalRound)
            {
                await AwardTournamentPrizesAsync(tournament, annotated, nameToTeam);
            }
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotated
            };
        }

        private async Task<List<string>> GetAllMemberIdsAsync(TeamDomain? team)
        {
            if (team == null)
            {
                return new List<string>();
            }
            var members = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var all = new HashSet<string>(members);
            if (!string.IsNullOrEmpty(team.CaptainId))
            {
                all.Add(team.CaptainId);
            }
            return all.ToList();
        }

        private async Task PersistRiotPlayerStatsAsync(MatchDomain matchEntity, RiotMatchOutcome outcome, string game)
        {
            foreach (var player in outcome.Players)
            {
                var playerStat = new PlayerMatchStatDomain
                {
                    MatchId = matchEntity.Id,
                    UserId = player.UserId,
                    Kills = player.Kills,
                    Deaths = player.Deaths,
                    Assists = player.Assists,
                    Game = game,
                    Champion = player.Champion
                };
                await _playerStatsRepository.AddPlayerMatchStatAsync(playerStat);

                var agg = await _playerStatsRepository.GetUserStatsAsync(player.UserId, game);
                if (agg == null)
                {
                    agg = new UserStatDomain
                    {
                        UserId = player.UserId,
                        Game = game,
                        TotalKills = player.Kills,
                        TotalDeaths = player.Deaths,
                        TotalAssists = player.Assists,
                        MatchesPlayed = 1
                    };
                    await _playerStatsRepository.AddUserStatsAsync(agg);
                }
                else
                {
                    agg.TotalKills += player.Kills;
                    agg.TotalDeaths += player.Deaths;
                    agg.TotalAssists += player.Assists;
                    agg.MatchesPlayed += 1;
                    await _playerStatsRepository.UpdateUserStats(agg);
                }
            }
        }

        private async Task AwardTournamentPrizesAsync(TournamentDomain tournament,List<List<MatchInfoDomain>> rounds,Dictionary<string, TeamDomain> teamNameMap)
        {
            bool alreadyAwarded = await _matchesRepository.IsTournamentAlreadyAwardedAsync(tournament.Id, rounds);

            if (alreadyAwarded) return;

            var finalMatch = rounds.Last().FirstOrDefault();
            if (finalMatch == null) return;

            TeamDomain? first = null;
            TeamDomain? second = null;

            if (finalMatch.Team1Score >= finalMatch.Team2Score)
            {
                teamNameMap.TryGetValue(finalMatch.Team1Name!, out first);
                teamNameMap.TryGetValue(finalMatch.Team2Name!, out second);
            }
            else
            {
                teamNameMap.TryGetValue(finalMatch.Team2Name!, out first);
                teamNameMap.TryGetValue(finalMatch.Team1Name!, out second);
            }

            TeamDomain? third = null;
            if (rounds.Count >= 2)
            {
                var semiFinals = rounds[^2];
                var losers = semiFinals
                    .Where(m => m.Team1Name != null && m.Team2Name != null)
                    .Select(m =>
                        m.Team1Score >= m.Team2Score
                            ? m.Team2Name
                            : m.Team1Name)
                    .ToList();

                if (losers.Count > 0 && teamNameMap.TryGetValue(losers[0]!, out var t))
                    third = t;
            }

            await AwardPrizeAsync(first, tournament.FirstPlacePrize, tournament.Id, "1st");
            await AwardPrizeAsync(second, tournament.SecondPlacePrize, tournament.Id, "2nd");
            await AwardPrizeAsync(third, tournament.ThirdPlacePrize, tournament.Id, "3rd");
        }

        private async Task AwardPrizeAsync(TeamDomain? team, string prize, int tournamentId, string place)
        {
            if (team == null || string.IsNullOrWhiteSpace(prize)) return;

            if (!prize.Contains("ClashCoins", StringComparison.OrdinalIgnoreCase))
                return;

            var match = System.Text.RegularExpressions.Regex.Match(prize, @"\d+");
            if (!match.Success || !int.TryParse(match.Value, out int amount) || amount <= 0)
                return;

            var members = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allIds = new HashSet<string>(members) { team.CaptainId };

            foreach (var userId in allIds)
            {
                await _coinWalletService.CreditAsync(
                    userId,
                    amount,
                    $"Tournament:{tournamentId}:{place}Place");
            }
        }

        private async Task GenerateStatsForMatchAsync(MatchDomain matchEntity, TeamDomain? team1, TeamDomain? team2, string game)
        {
            var random = new Random();
            var usedChampions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await GenerateTeamStatsAsync(matchEntity, team1, game, random, usedChampions);
            await GenerateTeamStatsAsync(matchEntity, team2, game, random, usedChampions);
        }

        private async Task GenerateTeamStatsAsync(MatchDomain matchEntity, TeamDomain? team, string game, Random random, ISet<string> usedChampions)
        {
            if (team == null) return;
            bool isLol = GameCatalog.IsLol(game);
            var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allMemberIds = new HashSet<string>(memberIds) { team.CaptainId };
            foreach (var userId in allMemberIds)
            {
                int kills, deaths, assists;
                string? champion = null;
                if (isLol)
                {
                    kills = random.Next(0, 16);
                    deaths = random.Next(0, 12);
                    assists = random.Next(0, 25);
                    champion = PickChampion(usedChampions, random);
                }
                else
                {
                    kills = random.Next(0, 21);
                    deaths = random.Next(0, 21);
                    assists = random.Next(0, 11);
                }
                var playerStat = new PlayerMatchStatDomain
                {
                    MatchId = matchEntity.Id,
                    UserId = userId,
                    Kills = kills,
                    Deaths = deaths,
                    Assists = assists,
                    Game = game,
                    Champion = champion
                };
                await _playerStatsRepository.AddPlayerMatchStatAsync(playerStat);
                var agg = await _playerStatsRepository.GetUserStatsAsync(userId, game);
                if (agg == null)
                {
                    agg = new UserStatDomain { UserId = userId, Game = game, TotalKills = kills, TotalDeaths = deaths, TotalAssists = assists, MatchesPlayed = 1 };
                    await _playerStatsRepository.AddUserStatsAsync(agg);
                }
                else
                {
                    agg.TotalKills += kills;
                    agg.TotalDeaths += deaths;
                    agg.TotalAssists += assists;
                    agg.MatchesPlayed += 1;
                    await _playerStatsRepository.UpdateUserStats(agg);
                }
            }
        }

        private static string PickChampion(ISet<string> usedChampions, Random random)
        {
            var available = GameCatalog.Champions.Where(c => !usedChampions.Contains(c)).ToList();
            if (available.Count == 0)
            {
                available = GameCatalog.Champions.ToList();
            }
            var champion = available[random.Next(available.Count)];
            usedChampions.Add(champion);
            return champion;
        }
        private static (int team1Score, int team2Score) GenerateMatchScores(string game, Random random)
        {
            if (GameCatalog.IsLol(game))
            {
                int loserMaps = random.Next(0, 2); 
                bool team1Wins = random.Next(0, 2) == 0;
                return team1Wins ? (2, loserMaps) : (loserMaps, 2);
            }
            return (random.Next(0, 16), random.Next(0, 16));
        }

        private static List<List<MatchInfoDomain>> GenerateBracketSkeleton(List<string> teamNames)
        {
            int n = teamNames.Count;
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? teamNames[i] : null;
            }
            var roundsList = new List<List<MatchInfoDomain>>();
            var initialWinners = new List<string?>();
            var firstRound = new List<MatchInfoDomain>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var t1 = slots[i];
                var t2 = slots[i + 1];
                if (t1 == null && t2 == null)
                {
                    initialWinners.Add(null);
                    continue;
                }
                firstRound.Add(new MatchInfoDomain { Team1Name = t1, Team2Name = t2 });
                initialWinners.Add(t1 ?? t2);
            }
            roundsList.Add(firstRound);
            var winners = initialWinners;
            while (winners.Count > 1)
            {
                int nextCount = (int)Math.Ceiling(winners.Count / 2.0);
                var nextRound = new List<MatchInfoDomain>();
                for (int i = 0; i < nextCount; i++)
                {
                    nextRound.Add(new MatchInfoDomain { Team1Name = null, Team2Name = null });
                }
                roundsList.Add(nextRound);
                var tmp = new List<string?>();
                for (int i = 0; i < nextCount; i++)
                {
                    tmp.Add(null);
                }
                winners = tmp;
            }
            return roundsList;
        }

        private async Task<List<List<MatchInfoDomain>>> BuildBracketWithDbAsync(int tournamentId, List<List<MatchInfoDomain>> skeleton, Dictionary<int, string> idToName)
        {
            var matches = await _matchesRepository.GetMatchesByTournamentAsync(tournamentId);
            var matchDict = new Dictionary<(string?, string?), MatchDomain>(new TeamPairComparer());
            foreach (var m in matches)
            {
                string? t1Name = null;
                string? t2Name = null;
                if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var n1))
                {
                    t1Name = n1;
                }
                if (m.Team2Id.HasValue && idToName.TryGetValue(m.Team2Id.Value, out var n2))
                {
                    t2Name = n2;
                }
                if (t1Name != null && t2Name != null)
                {
                    matchDict[(t1Name, t2Name)] = m;
                    matchDict[(t2Name, t1Name)] = m;
                }
            }
            var prevWinners = new List<(string? name, bool fromMatch)>();
            for (int i = 0; i < skeleton[0].Count; i++)
            {
                var mInfo = skeleton[0][i];
                var t1Name = mInfo.Team1Name;
                var t2Name = mInfo.Team2Name;
                if (!string.IsNullOrEmpty(t1Name) && !string.IsNullOrEmpty(t2Name))
                {
                    if (matchDict.TryGetValue((t1Name!, t2Name!), out var m))
                    {
                        if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var name1) && string.Equals(name1, t1Name, StringComparison.OrdinalIgnoreCase))
                        {
                            mInfo.Team1Score = m.Team1Score;
                            mInfo.Team2Score = m.Team2Score;
                            string winnerName = m.Team1Score >= m.Team2Score ? t1Name! : t2Name!;
                            prevWinners.Add((winnerName, true));
                        }
                        else
                        {
                            mInfo.Team1Score = m.Team2Score;
                            mInfo.Team2Score = m.Team1Score;
                            string winnerName = m.Team2Score >= m.Team1Score ? t1Name! : t2Name!;
                            prevWinners.Add((winnerName, true));
                        }
                    }
                    else
                    {
                        prevWinners.Add((null, false));
                    }
                }
                else if (!string.IsNullOrEmpty(t1Name) || !string.IsNullOrEmpty(t2Name))
                {
                    prevWinners.Add((t1Name ?? t2Name, false));
                }
                else
                {
                    prevWinners.Add((null, false));
                }
            }
            for (int r = 1; r < skeleton.Count; r++)
            {
                var currentWinners = new List<(string? name, bool fromMatch)>();
                for (int i = 0; i < skeleton[r].Count; i++)
                {
                    var idx1 = 2 * i;
                    var idx2 = 2 * i + 1;
                    (string? nameA, bool fromA) = idx1 < prevWinners.Count ? prevWinners[idx1] : (null, false);
                    (string? nameB, bool fromB) = idx2 < prevWinners.Count ? prevWinners[idx2] : (null, false);
                    var mInfo = skeleton[r][i];
                    if (!string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB) && fromA && fromB)
                    {
                        mInfo.Team1Name = nameA;
                        mInfo.Team2Name = nameB;
                        if (matchDict.TryGetValue((nameA!, nameB!), out var m))
                        {
                            if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var nm1) && string.Equals(nm1, nameA, StringComparison.OrdinalIgnoreCase))
                            {
                                mInfo.Team1Score = m.Team1Score;
                                mInfo.Team2Score = m.Team2Score;
                                string winnerName = m.Team1Score >= m.Team2Score ? nameA! : nameB!;
                                currentWinners.Add((winnerName, true));
                            }
                            else
                            {
                                mInfo.Team1Score = m.Team2Score;
                                mInfo.Team2Score = m.Team1Score;
                                string winnerName = m.Team2Score >= m.Team1Score ? nameA! : nameB!;
                                currentWinners.Add((winnerName, true));
                            }
                        }
                        else
                        {
                            currentWinners.Add((null, false));
                        }
                    }
                    else if (!string.IsNullOrEmpty(nameA) && string.IsNullOrEmpty(nameB))
                    {
                        currentWinners.Add((nameA, fromA));
                    }
                    else if (string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB))
                    {
                        currentWinners.Add((nameB, fromB));
                    }
                    else
                    {
                        currentWinners.Add((null, false));
                    }
                }
                prevWinners = currentWinners;
            }
            return skeleton;
        }

        private static List<List<MatchInfoDomain>> GenerateBracketWithResults(List<string> teamNames, string game)
        {
            var random = new Random();
            int byeScore = GameCatalog.IsLol(game) ? 2 : random.Next(0, 16);
            var shuffled = teamNames.OrderBy(_ => random.Next()).ToList();
            int n = shuffled.Count;
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? shuffled[i] : null;
            }
            var result = new List<List<MatchInfoDomain>>();
            var winners = new List<string?>();
            var firstRound = new List<MatchInfoDomain>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var t1 = slots[i];
                var t2 = slots[i + 1];
                if (t1 == null && t2 == null)
                {
                    winners.Add(null);
                    continue;
                }
                var m = new MatchInfoDomain { Team1Name = t1, Team2Name = t2 };
                if (t1 != null && t2 != null)
                {
                    var (s1, s2) = GenerateMatchScores(game, random);
                    m.Team1Score = s1;
                    m.Team2Score = s2;
                    winners.Add(s1 >= s2 ? t1 : t2);
                }
                else if (t1 != null)
                {
                    m.Team1Score = byeScore;
                    winners.Add(t1);
                }
                else if (t2 != null)
                {
                    m.Team2Score = byeScore;
                    winners.Add(t2);
                }
                else
                {
                    winners.Add(null);
                }
                firstRound.Add(m);
            }
            result.Add(firstRound);
            while (winners.Count > 1)
            {
                int nextCount = (int)Math.Ceiling(winners.Count / 2.0);
                var roundMatches = new List<MatchInfoDomain>();
                var nextWinners = new List<string?>();
                for (int i = 0; i < nextCount; i++)
                {
                    var idx1 = 2 * i;
                    var idx2 = 2 * i + 1;
                    var nameA = idx1 < winners.Count ? winners[idx1] : null;
                    var nameB = idx2 < winners.Count ? winners[idx2] : null;
                    if (nameA == null && nameB == null)
                    {
                        nextWinners.Add(null);
                        continue;
                    }
                    var m = new MatchInfoDomain();
                    if (nameA != null)
                    {
                        m.Team1Name = nameA;
                    }
                    if (nameB != null)
                    {
                        m.Team2Name = nameB;
                    }
                    if (nameA != null && nameB != null)
                    {
                        var (s1, s2) = GenerateMatchScores(game, random);
                        m.Team1Score = s1;
                        m.Team2Score = s2;
                        nextWinners.Add(s1 >= s2 ? nameA : nameB);
                    }
                    else
                    {
                        nextWinners.Add(nameA ?? nameB);
                    }
                    roundMatches.Add(m);
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            return AnnotateRounds(result);
        }

        private static List<List<MatchInfoDomain>> AnnotateRounds(List<List<MatchInfoDomain>> rounds)
        {
            var annotated = new List<List<MatchInfoDomain>>();
            for (int r = 0; r < rounds.Count; r++)
            {
                var roundList = new List<MatchInfoDomain>();
                int matchNum = 1;
                foreach (var match in rounds[r])
                {
                    match.Round = r + 1;
                    match.MatchNum = matchNum++;
                    roundList.Add(match);
                }
                annotated.Add(roundList);
            }
            return annotated;
        }

        private async Task SaveBracketStatsAsync(List<List<MatchInfoDomain>> rounds, TournamentDomain tournament, Dictionary<string, TeamDomain> teamNameMap)
        {
            foreach (var round in rounds)
            {
                foreach (var matchInfo in round)
                {
                    if (matchInfo.Team1Name == null && matchInfo.Team2Name == null) continue;
                    TeamDomain? team1 = null;
                    TeamDomain? team2 = null;
                    if (matchInfo.Team1Name != null && teamNameMap.TryGetValue(matchInfo.Team1Name, out var t1))
                    {
                        team1 = t1;
                    }
                    if (matchInfo.Team2Name != null && teamNameMap.TryGetValue(matchInfo.Team2Name, out var t2))
                    {
                        team2 = t2;
                    }
                    var match = new MatchDomain
                    {
                        TournamentId = tournament.Id,
                        Team1Id = team1?.Id,
                        Team2Id = team2?.Id,
                        Team1Score = matchInfo.Team1Score ?? 0,
                        Team2Score = matchInfo.Team2Score ?? 0,
                        PlayedAt = DateTime.UtcNow
                    };
                    await _matchesRepository.AddMatchAsync(match);
                    await GenerateStatsForMatchAsync(match, team1, team2, tournament.GameTitle);
                }
            }
        }

        #region Email Notifications
        private async Task NotifyInitialMatchesAsync(TournamentDomain tournament, Dictionary<string, TeamDomain> teamNameMap, List<List<MatchInfoDomain>> rounds)
        {
            if (rounds.Count == 0)
            {
                return;
            }
            var sends = new List<Func<Task>>();
            var firstRound = rounds[0];
            foreach (var match in firstRound)
            {
                if (!string.IsNullOrEmpty(match.Team1Name) && !string.IsNullOrEmpty(match.Team2Name))
                {
                    if (teamNameMap.TryGetValue(match.Team1Name!, out var team1))
                    {
                        foreach (var email in await GetTeamMemberEmailsAsync(team1))
                        {
                            sends.Add(() => _emailService.SendMatchReminderAsync(email, tournament.Name, match.Team2Name!, scheduledAt: null));
                        }
                    }
                    if (teamNameMap.TryGetValue(match.Team2Name!, out var team2))
                    {
                        foreach (var email in await GetTeamMemberEmailsAsync(team2))
                        {
                            sends.Add(() => _emailService.SendMatchReminderAsync(email, tournament.Name, match.Team1Name!, scheduledAt: null));
                        }
                    }
                }
            }
            using var throttle = new SemaphoreSlim(MaxConcurrentEmailSends);
            await Task.WhenAll(sends.Select(send => SafeSendAsync(send, throttle)));
        }

        private async Task<List<string>> GetTeamMemberEmailsAsync(TeamDomain team)
        {
            var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allMembers = new HashSet<string>(memberIds) { team.CaptainId };
            var emails = new List<string>();
            foreach (var memberId in allMembers)
            {
                var user = await _userRepository.FindByIdAsync(memberId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    emails.Add(user.Email);
                }
            }
            return emails;
        }

        private static async Task SafeSendAsync(Func<Task> send, SemaphoreSlim throttle)
        {
            await throttle.WaitAsync();
            try
            {
                await send();
            }
            catch
            {
            }
            finally
            {
                throttle.Release();
            }
        }
        #endregion
    }
}


public class TeamPairComparer : IEqualityComparer<(string? A, string? B)>
{
    private static readonly StringComparer C = StringComparer.OrdinalIgnoreCase;

    public bool Equals((string? A, string? B) x, (string? A, string? B) y) =>
        C.Equals(x.A ?? string.Empty, y.A ?? string.Empty) &&
        C.Equals(x.B ?? string.Empty, y.B ?? string.Empty);

    public int GetHashCode((string? A, string? B) obj) =>
        HashCode.Combine(
            C.GetHashCode(obj.A ?? string.Empty),
            C.GetHashCode(obj.B ?? string.Empty)
        );
}