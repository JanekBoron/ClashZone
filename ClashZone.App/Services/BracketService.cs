using ClashZone.App.Domains;
using ClashZone.App.Domains.Riot;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    /// <summary>
    /// Service responsible for generating and annotating tournament brackets.
    /// This implementation has been extended to support random per?match
    /// simulation, persisting statistics for individual matches and
    /// constructing a bracket view based on already played matches.
    /// </summary>
    public class BracketService : IBracketService
    {
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

        /// <summary>
        /// Generates a bracket structure for a tournament without any scores.  Only
        /// the first round contains team names; subsequent rounds remain blank
        /// until matches are played.  Played match results stored in the
        /// database will populate the appropriate scores and winners.
        /// </summary>

        /// in 
        public async Task<BracketViewModel?> GetBracketAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfoDomain>>()
                };
            }
            // Build a skeleton bracket without propagating winners
            var skeleton = GenerateBracketSkeleton(teamNames);
            // Populate scores/names from already played matches
            var bracket = await BuildBracketWithDbAsync(tournamentId, teamNames, skeleton, teams);
            var annotated = AnnotateRounds(bracket);
            // Send notifications only the first time the bracket is built (no matches recorded yet)
            var existingMatches = await _matchesRepository.GetMatchesByTournamentAsync(tournamentId);
            if (existingMatches == null || !existingMatches.Any())
            {
                await NotifyInitialMatchesAsync(tournament, teams, annotated);
            }
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotated
            };
        }

        /// <summary>
        /// Generates a bracket with random scores for every match.  All matches
        /// in the bracket are simulated in memory; no results are persisted and
        /// participant names are propagated automatically through the rounds.
        /// Empty pairings (two byes) are omitted.
        /// </summary>

        //in
        public async Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfoDomain>>()
                };
            }
            var rounds = GenerateBracketWithResults(teamNames, tournament.GameTitle);
            //zapisanie statystyk do bazy
            //await SaveBracketStatsAsync(rounds, tournament, teams);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        /// <summary>
        /// Generates a bracket with random scores and persists the results
        /// and random player statistics to the database.  This method uses
        /// GenerateBracketWithResults internally and then records the results
        /// in the persistence layer.
        /// </summary>
        /// 

        //Useless
        public async Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfoDomain>>()
                };
            }
            var rounds = GenerateBracketWithResults(teamNames, tournament.GameTitle);
            // Persist match results and generate random stats
            await SaveBracketStatsAsync(rounds, tournament, teams);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        /// <summary>
        /// Simulates a single match within the bracket.  The specified match must have
        /// both participants determined and not yet have a score.  Random scores
        /// are generated, persisted and player statistics recorded.  The updated
        /// bracket reflecting the new result is returned.  If the match cannot be
        /// simulated (due to missing participants or existing scores) null is returned.
        /// </summary>

        //in
        public async Task<BracketViewModel?> SimulateMatchAsync(int tournamentId, int roundNumber, int matchNumber, bool isAdmin = false)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            var nameToTeam = new Dictionary<string, TeamDomain>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    teamNames.Add(name);
                    if (!nameToTeam.ContainsKey(name))
                    {
                        nameToTeam[name] = team;
                    }
                }
            }
            if (teamNames.Count < 2)
            {
                return null;
            }
            var skeleton = GenerateBracketSkeleton(teamNames);
            var bracket = await BuildBracketWithDbAsync(tournamentId, teamNames, skeleton, teams);
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
            // Only simulate if both participants are defined and no score exists
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
                // League of Legends: wynik i statystyki pochodzą z prawdziwego meczu
                // pobranego z Riot API (a nie z losowania jak w CS2).
                var team1Members = await GetAllMemberIdsAsync(team1);
                var team2Members = await GetAllMemberIdsAsync(team2);
                // Gdy symulację odpala admin, wymuszamy ścieżkę deweloperską Riota.
                var outcome = await _riotMatchService.ResolveMatchAsync(team1Members, team2Members, forceDevMode: isAdmin);
                if (outcome == null)
                {
                    // Nie udało się ustalić rozegranego meczu (brak powiązanych kont
                    // Riot, brak wspólnej gry lub wyłączona integracja) – nic nie zapisujemy.
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
                // Counter-Strike 2 (i inne gry): losowy wynik + losowe statystyki.
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
                // generate player stats for the single match
                await GenerateStatsForMatchAsync(matchEntity, team1, team2, tournament.GameTitle);
            }
            // rebuild bracket to reflect new result
            bracket = await BuildBracketWithDbAsync(tournamentId, teamNames, skeleton, teams);
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


        /// <summary>
        /// Zwraca identyfikatory wszystkich graczy drużyny (członkowie + kapitan).
        /// Używane do zmapowania składu na konta Riot przy pobieraniu meczu LoL.
        /// </summary>
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

        /// <summary>
        /// Zapisuje statystyki graczy pochodzące z prawdziwego meczu Riota oraz
        /// aktualizuje agregaty użytkownika. Odpowiednik <see cref="GenerateTeamStatsAsync"/>,
        /// ale dane pochodzą z API, a nie z losowania.
        /// </summary>
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
            // Avoid double awarding
            bool alreadyAwarded = await _matchesRepository.IsTournamentAlreadyAwardedAsync(tournament.Id, rounds);

            if (alreadyAwarded) return;

            // FINAL
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

            // THIRD PLACE (losers of semifinals)
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



        /// <summary>
        /// Helper to generate random per?player statistics for a single match and
        /// update aggregated user stats.
        /// </summary>

        //in
        private async Task GenerateStatsForMatchAsync(MatchDomain matchEntity, TeamDomain? team1, TeamDomain? team2, string game)
        {
            var random = new Random();
            // Postacie (championy) są unikalne w obrębie jednego meczu (tylko LoL).
            var usedChampions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await GenerateTeamStatsAsync(matchEntity, team1, game, random, usedChampions);
            await GenerateTeamStatsAsync(matchEntity, team2, game, random, usedChampions);
        }

        /// <summary>
        /// Generuje i zapisuje losowe statystyki dla jednej drużyny w meczu,
        /// uwzględniając specyfikę gry: dla League of Legends każdy gracz dostaje
        /// inną postać i inne zakresy KDA niż w Counter-Strike. Agreguje też
        /// statystyki użytkownika w rozbiciu na grę.
        /// </summary>
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
                    // typowe zakresy KDA dla pojedynczej gry LoL
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

        /// <summary>
        /// Losuje postać (champion) jeszcze nieużytą w danym meczu. Gdy pula się
        /// wyczerpie, dopuszcza powtórzenia.
        /// </summary>
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

        /// <summary>
        /// Generuje wynik pojedynczego meczu zależnie od gry. Counter-Strike używa
        /// rund (0–15), League of Legends serii Bo3 (zwycięzca 2 mapy, przegrany 0–1).
        /// </summary>
        private static (int team1Score, int team2Score) GenerateMatchScores(string game, Random random)
        {
            if (GameCatalog.IsLol(game))
            {
                int loserMaps = random.Next(0, 2); // 0 lub 1
                bool team1Wins = random.Next(0, 2) == 0;
                return team1Wins ? (2, loserMaps) : (loserMaps, 2);
            }
            return (random.Next(0, 16), random.Next(0, 16));
        }

        /// <summary>
        /// Generates a basic bracket skeleton.  Only the first round contains
        /// team names; subsequent rounds contain empty slots.  Pairs where both
        /// entries are null are omitted from the first round.  A placeholder for
        /// the winners list is maintained internally to compute the correct number
        /// of matches in later rounds.
        /// </summary>

        //in
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

        /// <summary>
        /// Builds a bracket from the provided skeleton and fills in scores/names
        /// based on matches stored in the database.  Only matches that have been
        /// played will populate participant names and scores in later rounds.
        /// Otherwise names remain null to hide upcoming pairings until they are
        /// determined by previous matches.
        /// </summary>

        //in
        private async Task<List<List<MatchInfoDomain>>> BuildBracketWithDbAsync(int tournamentId, List<string> teamNames, List<List<MatchInfoDomain>> skeleton, List<TeamDomain> teams)
        {
            // mapping between team IDs and their display names
            var idToName = new Dictionary<int, string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    idToName[team.Id] = name;
                }
            }
            // Fetch all played matches for this tournament
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
            // Determine winners for the first round
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
                        // orientation: if stored Team1Id matches first name
                        if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var name1) && string.Equals(name1, t1Name, StringComparison.OrdinalIgnoreCase))
                        {
                            mInfo.Team1Score = m.Team1Score;
                            mInfo.Team2Score = m.Team2Score;
                            string winnerName = m.Team1Score >= m.Team2Score ? t1Name! : t2Name!;
                            prevWinners.Add((winnerName, true));
                        }
                        else
                        {
                            // reversed orientation
                            mInfo.Team1Score = m.Team2Score;
                            mInfo.Team2Score = m.Team1Score;
                            string winnerName = m.Team2Score >= m.Team1Score ? t1Name! : t2Name!;
                            prevWinners.Add((winnerName, true));
                        }
                    }
                    else
                    {
                        // not yet played
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
            // Build subsequent rounds
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
                    // match ready only when both participants originate from played matches
                    if (!string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB) && fromA && fromB)
                    {
                        mInfo.Team1Name = nameA;
                        mInfo.Team2Name = nameB;
                        if (matchDict.TryGetValue((nameA!, nameB!), out var m))
                        {
                            // fill scores and compute winner
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
                        // Bye: participant automatically advances but names stay hidden
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

        /// <summary>
        /// Generates a bracket with random scores across all rounds.  Random values
        /// are assigned only for matches where both participants exist.  Empty
        /// pairings consisting of two byes are skipped.  Names propagate to later
        /// rounds automatically.
        /// </summary>
        /// 
        //in
        private static List<List<MatchInfoDomain>> GenerateBracketWithResults(List<string> teamNames, string game)
        {
            var random = new Random();
            // Wynik dla drużyny przechodzącej dalej bez gry (bye).
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
            // first round
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
            // subsequent rounds
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

        /// <summary>
        /// Assigns round and match numbers to each match in the bracket.  This helper
        /// remains unchanged from the original implementation.
        /// </summary>

        //in
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

        /// <summary>
        /// Persists randomly generated results and statistics for each match in the
        /// provided bracket.  This logic is largely unchanged from the original
        /// implementation but now avoids persisting completely empty matches.
        /// </summary>

        //in
        private async Task SaveBracketStatsAsync(List<List<MatchInfoDomain>> rounds, TournamentDomain tournament, List<TeamDomain> teams)
        {
            // Determine if there were any matches before persisting new ones.
            bool hadExistingMatches = await _matchesRepository.TournamentHasMatchesAsync(tournament.Id);

            // Build dictionary from team display names to team objects
            var teamNameMap = new Dictionary<string, TeamDomain>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    teamNameMap[name] = team;
                }
            }
            foreach (var round in rounds)
            {
                foreach (var matchInfo in round)
                {
                    // Only persist matches where at least one team is present
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
                    // Generuje statystyki obu drużyn (zależne od gry) przez wspólny helper.
                    await GenerateStatsForMatchAsync(match, team1, team2, tournament.GameTitle);
                }
            }

            // Award prizes after all matches have been persisted. Only award if
            // there were no matches recorded prior to this invocation to avoid
            // duplicating prizes on subsequent bracket updates.
            if (!hadExistingMatches)
            {
                // Determine winner from the final match of the last round
                if (rounds != null && rounds.Count > 0)
                {
                    var lastRound = rounds[rounds.Count - 1];
                    if (lastRound.Count > 0)
                    {
                        var finalMatch = lastRound[0];
                        string? winnerName = null;
                        int finalScore1 = finalMatch.Team1Score ?? 0;
                        int finalScore2 = finalMatch.Team2Score ?? 0;
                        if (finalScore1 >= finalScore2)
                        {
                            winnerName = finalMatch.Team1Name;
                        }
                        else
                        {
                            winnerName = finalMatch.Team2Name;
                        }
                        if (!string.IsNullOrEmpty(winnerName) && teamNameMap.TryGetValue(winnerName, out var winningTeam))
                        {
                           /* string prize = tournament.Prize ?? string.Empty;
                            var lowerPrize = prize.ToLowerInvariant();
                            // Consider the prize as ClashCoins if it mentions "clash"
                            if (lowerPrize.Contains("clash"))
                            {
                                var match = System.Text.RegularExpressions.Regex.Match(prize, @"\d+");
                                if (match.Success && int.TryParse(match.Value, out int amount) && amount > 0)
                                {
                                    // Credit the parsed amount to each member of the winning team
                                    var memberIdsPrize = await _tournamentsRepository.GetTeamMemberIdsAsync(winningTeam.Id);
                                    var allIdsPrize = new HashSet<string>(memberIdsPrize) { winningTeam.CaptainId };
                                    foreach (var userId in allIdsPrize)
                                    {
                                        await _coinWalletService.CreditAsync(userId, amount, $"Tournament:{tournament.Id}:1stPlace");
                                    }
                                }
                            }*/
                        }
                    }
                }
            }
        }

        #region Email Notifications
        // The methods below are from the original implementation and handle
        // notifying teams of their first round matches and byes.  They are
        // retained unchanged to preserve existing functionality.
        private async Task NotifyInitialMatchesAsync(TournamentDomain tournament, List<TeamDomain> teams, List<List<MatchInfoDomain>> rounds)
        {
            if (rounds.Count == 0)
            {
                return;
            }
            var teamNameMap = new Dictionary<string, TeamDomain>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userRepository.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    teamNameMap[name] = team;
                }
            }
            var firstRound = rounds[0];
            foreach (var match in firstRound)
            {
                if (!string.IsNullOrEmpty(match.Team1Name) && !string.IsNullOrEmpty(match.Team2Name))
                {
                    if (teamNameMap.TryGetValue(match.Team1Name!, out var team1))
                    {
                        await NotifyTeamMatchAsync(team1, match.Team2Name!, tournament);
                    }
                    if (teamNameMap.TryGetValue(match.Team2Name!, out var team2))
                    {
                        await NotifyTeamMatchAsync(team2, match.Team1Name!, tournament);
                    }
                }
                else
                {
                    var advancingName = match.Team1Name ?? match.Team2Name;
                    if (!string.IsNullOrEmpty(advancingName) && teamNameMap.TryGetValue(advancingName, out var team))
                    {
                        await NotifyTeamAdvanceAsync(team, tournament, 2);
                    }
                }
            }
        }

        private async Task NotifyTeamMatchAsync(TeamDomain team, string opponentName, TournamentDomain tournament)
        {
            var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allMembers = new HashSet<string>(memberIds) { team.CaptainId };
            foreach (var memberId in allMembers)
            {
                var user = await _userRepository.FindByIdAsync(memberId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendMatchReminderAsync(user.Email, tournament.Name, opponentName, scheduledAt: null);
                }
            }
        }

        private async Task NotifyTeamAdvanceAsync(TeamDomain team, TournamentDomain tournament, int round)
        {
            var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allMembers = new HashSet<string>(memberIds) { team.CaptainId };
            foreach (var memberId in allMembers)
            {
                var user = await _userRepository.FindByIdAsync(memberId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendPromotionNotificationAsync(user.Email, tournament.Name, round);
                }
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