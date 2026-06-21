using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    public class LeagueService : ILeagueService
    {
        private readonly ILeagueRepository _leagueRepository;
        private readonly ILeagueTeamsRepository _teamsRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPlayerStatsRepository _playerStatsRepository;
        private readonly ICoinWalletService _coinWalletService;

        public LeagueService(
            ILeagueRepository leagueRepository,
            ILeagueTeamsRepository teamsRepository,
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository,
            IPlayerStatsRepository playerStatsRepository,
            ICoinWalletService coinWalletService)
        {
            _leagueRepository = leagueRepository;
            _teamsRepository = teamsRepository;
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _playerStatsRepository = playerStatsRepository;
            _coinWalletService = coinWalletService;
        }

        public Task<List<SeasonDomain>> GetSeasonsAsync() => _leagueRepository.GetSeasonsAsync();

        public Task<List<SeasonDomain>> GetManageListAsync() => _leagueRepository.GetSeasonsAsync();

        public async Task<SeasonDetailsViewModel?> GetSeasonDetailsAsync(int id, string? userId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(id);
            if (season == null) return null;

            var registrations = await _leagueRepository.GetRegistrationsForSeasonAsync(id);
            var vm = new SeasonDetailsViewModel
            {
                Season = season,
                RegisteredTeamsCount = registrations.Count,
                Standings = await GetStandingsAsync(id)
            };

            if (!string.IsNullOrEmpty(userId))
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
                vm.HasLeagueAccess = subscription?.Plan?.IsLeagueAccess == true;

                var userTeams = await _teamsRepository.GetUserTeamsAsync(userId);
                var registeredTeamIds = registrations.Select(r => r.LeagueTeamId).ToHashSet();
                vm.IsRegistered = userTeams.Any(t => registeredTeamIds.Contains(t.Id));
                vm.EligibleTeams = userTeams
                    .Where(t => t.CaptainId == userId
                                && string.Equals(t.Game, season.Game, StringComparison.OrdinalIgnoreCase)
                                && !registeredTeamIds.Contains(t.Id))
                    .ToList();
            }
            return vm;
        }

        public async Task CreateSeasonAsync(SeasonFormViewModel model, string createdByUserId)
        {
            var season = MapForm(model);
            season.Status = (int)SeasonStatus.Registration;
            season.CreatedByUserId = createdByUserId;
            season.Game = GameCatalog.Normalize(model.Game) ?? model.Game;
            await _leagueRepository.AddSeasonAsync(season);
        }

        public async Task UpdateSeasonAsync(SeasonFormViewModel model)
        {
            var existing = await _leagueRepository.GetSeasonByIdAsync(model.Id);
            if (existing == null) return;
            var updated = MapForm(model);
            updated.Id = model.Id;
            updated.Status = existing.Status;
            updated.CreatedByUserId = existing.CreatedByUserId;
            updated.Game = GameCatalog.Normalize(model.Game) ?? model.Game;
            await _leagueRepository.UpdateSeasonAsync(updated);
        }

        public async Task<SeasonFormViewModel?> GetSeasonFormAsync(int id)
        {
            var s = await _leagueRepository.GetSeasonByIdAsync(id);
            if (s == null) return null;
            return new SeasonFormViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Game = s.Game,
                Format = s.Format,
                RegistrationOpenDate = s.RegistrationOpenDate,
                RegistrationCloseDate = s.RegistrationCloseDate,
                StartDate = s.StartDate,
                RosterSize = s.RosterSize,
                PlayoffSpots = s.PlayoffSpots,
                FirstPlacePrize = s.FirstPlacePrize,
                SecondPlacePrize = s.SecondPlacePrize,
                ThirdPlacePrize = s.ThirdPlacePrize,
                DefaultMatchDay = s.DefaultMatchDay,
                Rules = s.Rules
            };
        }

        private static SeasonDomain MapForm(SeasonFormViewModel m) => new()
        {
            Name = m.Name,
            Game = m.Game,
            Format = m.Format,
            RegistrationOpenDate = m.RegistrationOpenDate,
            RegistrationCloseDate = m.RegistrationCloseDate,
            StartDate = m.StartDate,
            RosterSize = m.RosterSize,
            PlayoffSpots = m.PlayoffSpots,
            FirstPlacePrize = m.FirstPlacePrize,
            SecondPlacePrize = m.SecondPlacePrize,
            ThirdPlacePrize = m.ThirdPlacePrize,
            DefaultMatchDay = m.DefaultMatchDay,
            Rules = m.Rules
        };

        public async Task<LeagueRegistrationResult> RegisterTeamAsync(int seasonId, int leagueTeamId, string userId)
        {
            var result = new LeagueRegistrationResult();
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null)
            {
                result.SeasonNotFound = true;
                return result;
            }
            result.RequiredRoster = season.RosterSize;

            var now = DateTime.UtcNow;
            if (season.Status != (int)SeasonStatus.Registration
                || now < season.RegistrationOpenDate || now > season.RegistrationCloseDate)
            {
                result.RegistrationClosed = true;
                return result;
            }

            var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
            if (subscription?.Plan?.IsLeagueAccess != true)
            {
                result.RequiresLeaguePackage = true;
                return result;
            }

            var team = await _teamsRepository.GetByIdAsync(leagueTeamId);
            if (team == null || team.CaptainId != userId)
            {
                result.NotCaptain = true;
                return result;
            }

            var existing = await _leagueRepository.GetRegistrationAsync(seasonId, leagueTeamId);
            if (existing != null && !existing.IsRemoved)
            {
                result.AlreadyRegistered = true;
                return result;
            }

            var memberCount = (await _teamsRepository.GetMemberIdsAsync(leagueTeamId)).Count;
            if (memberCount < season.RosterSize)
            {
                result.RosterTooSmall = true;
                return result;
            }

            await _leagueRepository.AddRegistrationAsync(new LeagueRegistrationDomain
            {
                SeasonId = seasonId,
                LeagueTeamId = leagueTeamId,
                RegisteredAt = now,
                FeePaid = true 
            });
            result.Success = true;
            return result;
        }

        public async Task CloseRegistrationAndPruneAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return;

            var registrations = await _leagueRepository.GetRegistrationsForSeasonAsync(seasonId);
            foreach (var reg in registrations)
            {
                var rosterCount = (await _teamsRepository.GetMemberIdsAsync(reg.LeagueTeamId)).Count;
                if (!reg.FeePaid || rosterCount < season.RosterSize)
                {
                    await _leagueRepository.SetRemovedAsync(reg.Id, true);
                }
            }
            season.Status = (int)SeasonStatus.RegularSeason;
            await _leagueRepository.UpdateSeasonAsync(season);
        }

        public async Task GenerateOpeningMatchupsAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return;
            var regs = (await _leagueRepository.GetRegistrationsForSeasonAsync(seasonId)).Select(r => r.Id).ToList();
            if (regs.Count < 2) return;

            var existing = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            if (existing.Any(m => !m.IsPlayoff)) return; 

            var rng = new Random();
            var pairKeys = new HashSet<string>();
            await GenerateWeekMatchesAsync(season, 1, () => regs.OrderBy(_ => rng.Next()).ToList(), pairKeys);
        }

        public async Task GenerateNextWeekAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return;
            var matches = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            var regular = matches.Where(m => !m.IsPlayoff).ToList();
            if (!regular.Any()) { await GenerateOpeningMatchupsAsync(seasonId); return; }
            if (regular.Any(m => !m.IsPlayed)) return; 

            int nextWeek = regular.Max(m => m.Week) + 1;
            var standings = await GetStandingsAsync(seasonId);
            var orderedRegIds = standings.Select(s => s.RegistrationId).ToList();
            var pairKeys = new HashSet<string>();
            foreach (var m in regular.Where(m => m.HomeRegistrationId.HasValue && m.AwayRegistrationId.HasValue))
            {
                pairKeys.Add(PairKey(m.HomeRegistrationId!.Value, m.AwayRegistrationId!.Value));
                pairKeys.Add(PairKey(m.AwayRegistrationId!.Value, m.HomeRegistrationId!.Value));
            }
            await GenerateWeekMatchesAsync(season, nextWeek, () => new List<int>(orderedRegIds), pairKeys);
        }

        private async Task GenerateWeekMatchesAsync(SeasonDomain season, int week, Func<List<int>> orderProvider, HashSet<string> pairKeys)
        {
            var baseTime = season.StartDate.AddDays((week - 1) * 7);
            for (int round = 0; round < 2; round++)
            {
                var ordered = orderProvider();
                var pairs = BuildRoundPairs(ordered, pairKeys);
                foreach (var (home, away) in pairs)
                {
                    await _leagueRepository.AddMatchAsync(new LeagueMatchDomain
                    {
                        SeasonId = season.Id,
                        Week = week,
                        HomeRegistrationId = home,
                        AwayRegistrationId = away,
                        ScheduledTime = baseTime.AddDays(round),
                        IsPlayoff = false,
                        IsPlayed = false
                    });
                }
            }
        }

        private static List<(int? home, int? away)> BuildRoundPairs(List<int> ordered, HashSet<string> existing)
        {
            var remaining = new List<int>(ordered);
            var pairs = new List<(int?, int?)>();
            while (remaining.Count > 1)
            {
                int a = remaining[0];
                remaining.RemoveAt(0);
                int idx = remaining.FindIndex(b => !existing.Contains(PairKey(a, b)));
                if (idx < 0) idx = 0;
                int opp = remaining[idx];
                remaining.RemoveAt(idx);
                pairs.Add((a, opp));
                existing.Add(PairKey(a, opp));
                existing.Add(PairKey(opp, a));
            }
            if (remaining.Count == 1)
            {
                pairs.Add((remaining[0], null));
            }
            return pairs;
        }

        private static string PairKey(int a, int b) => $"{a}-{b}";

        public async Task GenerateResultsAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return;
            var rng = new Random();
            var matches = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            foreach (var match in matches.Where(m => !m.IsPlayoff && !m.IsPlayed))
            {
                await PlayMatchAsync(season, match, rng);
            }
        }

        private async Task PlayMatchAsync(SeasonDomain season, LeagueMatchDomain match, Random rng)
        {
            if (!match.AwayRegistrationId.HasValue)
            {
                match.HomeScore = GameCatalog.IsLol(season.Game) ? 2 : 16;
                match.AwayScore = 0;
                match.IsPlayed = true;
                await _leagueRepository.UpdateMatchAsync(match);
                return;
            }
            var (s1, s2) = GenerateScores(season.Game, rng);
            match.HomeScore = s1;
            match.AwayScore = s2;
            match.IsPlayed = true;
            await _leagueRepository.UpdateMatchAsync(match);

            var usedChampions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await GenerateRosterStatsAsync(match, match.HomeRegistrationId, season.Game, rng, usedChampions);
            await GenerateRosterStatsAsync(match, match.AwayRegistrationId, season.Game, rng, usedChampions);
        }

        private static (int, int) GenerateScores(string game, Random rng)
        {
            if (GameCatalog.IsLol(game))
            {
                int loserMaps = rng.Next(0, 2);
                bool homeWins = rng.Next(0, 2) == 0;
                return homeWins ? (2, loserMaps) : (loserMaps, 2);
            }
            return (rng.Next(0, 16), rng.Next(0, 16));
        }

        private async Task GenerateRosterStatsAsync(LeagueMatchDomain match, int? registrationId, string game, Random rng, ISet<string> usedChampions)
        {
            if (!registrationId.HasValue) return;
            var reg = await _leagueRepository.GetRegistrationByIdAsync(registrationId.Value);
            if (reg == null) return;
            var memberIds = await _teamsRepository.GetMemberIdsAsync(reg.LeagueTeamId);
            bool isLol = GameCatalog.IsLol(game);
            foreach (var userId in memberIds.Distinct())
            {
                int kills, deaths, assists;
                string? champion = null;
                if (isLol)
                {
                    kills = rng.Next(0, 16);
                    deaths = rng.Next(0, 12);
                    assists = rng.Next(0, 25);
                    champion = PickChampion(usedChampions, rng);
                }
                else
                {
                    kills = rng.Next(0, 21);
                    deaths = rng.Next(0, 21);
                    assists = rng.Next(0, 11);
                }
                await _playerStatsRepository.AddPlayerMatchStatAsync(new PlayerMatchStatDomain
                {
                    LeagueMatchId = match.Id,
                    UserId = userId,
                    Kills = kills,
                    Deaths = deaths,
                    Assists = assists,
                    Game = game,
                    Champion = champion
                });
                var agg = await _playerStatsRepository.GetUserStatsAsync(userId, game);
                if (agg == null)
                {
                    await _playerStatsRepository.AddUserStatsAsync(new UserStatDomain
                    {
                        UserId = userId,
                        Game = game,
                        TotalKills = kills,
                        TotalDeaths = deaths,
                        TotalAssists = assists,
                        MatchesPlayed = 1
                    });
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

        private static string PickChampion(ISet<string> used, Random rng)
        {
            var available = GameCatalog.Champions.Where(c => !used.Contains(c)).ToList();
            if (available.Count == 0) available = GameCatalog.Champions.ToList();
            var champion = available[rng.Next(available.Count)];
            used.Add(champion);
            return champion;
        }

        public async Task<List<LeagueStandingRow>> GetStandingsAsync(int seasonId)
        {
            var registrations = await _leagueRepository.GetRegistrationsForSeasonAsync(seasonId);
            var matches = (await _leagueRepository.GetMatchesForSeasonAsync(seasonId))
                .Where(m => !m.IsPlayoff && m.IsPlayed)
                .ToList();

            var rows = new List<LeagueStandingRow>();
            foreach (var reg in registrations)
            {
                int wins = 0, losses = 0, played = 0;
                foreach (var m in matches)
                {
                    if (m.HomeRegistrationId == reg.Id)
                    {
                        played++;
                        if (m.HomeScore >= m.AwayScore) wins++; else losses++;
                    }
                    else if (m.AwayRegistrationId == reg.Id)
                    {
                        played++;
                        if (m.AwayScore > m.HomeScore) wins++; else losses++;
                    }
                }
                rows.Add(new LeagueStandingRow
                {
                    RegistrationId = reg.Id,
                    TeamName = await ResolveRegistrationNameAsync(reg),
                    Played = played,
                    Wins = wins,
                    Losses = losses
                });
            }
            return rows.OrderByDescending(r => r.WinRatio).ThenByDescending(r => r.Wins).ThenBy(r => r.TeamName).ToList();
        }

        private async Task<string> ResolveRegistrationNameAsync(LeagueRegistrationDomain reg)
        {
            if (reg.LeagueTeam != null && !string.IsNullOrWhiteSpace(reg.LeagueTeam.Name))
                return reg.LeagueTeam.Name!;
            var captainId = reg.LeagueTeam?.CaptainId;
            if (!string.IsNullOrEmpty(captainId))
            {
                var captain = await _userRepository.FindByIdAsync(captainId);
                if (captain?.UserName != null) return $"team_{captain.UserName}";
            }
            return $"Team_{reg.LeagueTeamId}";
        }

        public async Task GeneratePlayoffsAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return;
            var existing = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            if (existing.Any(m => m.IsPlayoff)) return;

            var standings = await GetStandingsAsync(seasonId);
            int spots = Math.Min(season.PlayoffSpots, standings.Count);
            if (spots < 2) return;
            var seeds = standings.Take(spots).Select(s => s.RegistrationId).ToList();

            int i = 0, j = seeds.Count - 1;
            var rng = new Random();
            while (i < j)
            {
                await CreatePlayoffMatchAsync(season, 1, seeds[i], seeds[j], rng);
                i++; j--;
            }
            if (i == j)
            {
                await CreatePlayoffMatchAsync(season, 1, seeds[i], null, rng);
            }

            season.Status = (int)SeasonStatus.Playoffs;
            await _leagueRepository.UpdateSeasonAsync(season);
        }

        private async Task CreatePlayoffMatchAsync(SeasonDomain season, int round, int? home, int? away, Random rng)
        {
            var match = new LeagueMatchDomain
            {
                SeasonId = season.Id,
                Week = 0,
                Round = round,
                IsPlayoff = true,
                HomeRegistrationId = home,
                AwayRegistrationId = away,
                ScheduledTime = season.StartDate.AddDays(7 * 8 + round) 
            };
            if (!away.HasValue)
            {
                match.HomeScore = GameCatalog.IsLol(season.Game) ? 2 : 16;
                match.AwayScore = 0;
                match.IsPlayed = true;
            }
            await _leagueRepository.AddMatchAsync(match);
        }

        public async Task SimulatePlayoffMatchAsync(int seasonId, int matchId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return;
            var match = await _leagueRepository.GetMatchByIdAsync(matchId);
            if (match == null || !match.IsPlayoff || match.IsPlayed) return;
            if (!match.HomeRegistrationId.HasValue || !match.AwayRegistrationId.HasValue) return;

            var rng = new Random();
            await PlayMatchAsync(season, match, rng);

            var all = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            int round = match.Round ?? 1;
            var roundMatches = all.Where(m => m.IsPlayoff && (m.Round ?? 1) == round).OrderBy(m => m.Id).ToList();
            if (roundMatches.Any(m => !m.IsPlayed)) return;

            var winners = roundMatches.Select(WinnerOf).Where(w => w.HasValue).Select(w => w!.Value).ToList();
            if (winners.Count <= 1)
            {
                await AwardPlayoffPrizesAsync(season, all);
                season.Status = (int)SeasonStatus.Completed;
                await _leagueRepository.UpdateSeasonAsync(season);
                return;
            }

            if (!all.Any(m => m.IsPlayoff && (m.Round ?? 1) == round + 1))
            {
                for (int k = 0; k + 1 < winners.Count; k += 2)
                {
                    await CreatePlayoffMatchAsync(season, round + 1, winners[k], winners[k + 1], rng);
                }
                if (winners.Count % 2 == 1)
                {
                    await CreatePlayoffMatchAsync(season, round + 1, winners[^1], null, rng);
                }
            }
        }

        public async Task<bool> SimulateNextPlayoffMatchAsync(int seasonId)
        {
            var matches = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            var next = matches.FirstOrDefault(m => m.IsPlayoff && !m.IsPlayed
                && m.HomeRegistrationId.HasValue && m.AwayRegistrationId.HasValue);
            if (next == null) return false;
            await SimulatePlayoffMatchAsync(seasonId, next.Id);
            return true;
        }

        private static int? WinnerOf(LeagueMatchDomain m)
        {
            if (!m.HomeRegistrationId.HasValue) return m.AwayRegistrationId;
            if (!m.AwayRegistrationId.HasValue) return m.HomeRegistrationId;
            return m.HomeScore >= m.AwayScore ? m.HomeRegistrationId : m.AwayRegistrationId;
        }

        private async Task AwardPlayoffPrizesAsync(SeasonDomain season, List<LeagueMatchDomain> playoffMatches)
        {
            var playoffs = playoffMatches.Where(m => m.IsPlayoff).ToList();
            if (!playoffs.Any()) return;
            int finalRound = playoffs.Max(m => m.Round ?? 1);
            var final = playoffs.FirstOrDefault(m => (m.Round ?? 1) == finalRound);
            if (final == null) return;

            int? first = WinnerOf(final);
            int? second = first == final.HomeRegistrationId ? final.AwayRegistrationId : final.HomeRegistrationId;

            int? third = null;
            if (finalRound > 1)
            {
                var semis = playoffs.Where(m => (m.Round ?? 1) == finalRound - 1).ToList();
                var loser = semis.Select(m => WinnerOf(m) == m.HomeRegistrationId ? m.AwayRegistrationId : m.HomeRegistrationId)
                                 .FirstOrDefault(x => x.HasValue);
                third = loser;
            }

            await AwardPrizeAsync(first, season.FirstPlacePrize, season.Id, "1st");
            await AwardPrizeAsync(second, season.SecondPlacePrize, season.Id, "2nd");
            await AwardPrizeAsync(third, season.ThirdPlacePrize, season.Id, "3rd");
        }

        private async Task AwardPrizeAsync(int? registrationId, string prize, int seasonId, string place)
        {
            if (!registrationId.HasValue || string.IsNullOrWhiteSpace(prize)) return;
            if (!prize.Contains("ClashCoins", StringComparison.OrdinalIgnoreCase)) return;
            var match = System.Text.RegularExpressions.Regex.Match(prize, @"\d+");
            if (!match.Success || !int.TryParse(match.Value, out int amount) || amount <= 0) return;

            var reg = await _leagueRepository.GetRegistrationByIdAsync(registrationId.Value);
            if (reg == null) return;
            var memberIds = await _teamsRepository.GetMemberIdsAsync(reg.LeagueTeamId);
            foreach (var userId in memberIds.Distinct())
            {
                await _coinWalletService.CreditAsync(userId, amount, $"League:{seasonId}:{place}Place");
            }
        }

        public async Task<LeagueMatchesViewModel?> GetMatchesAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return null;
            var matches = await _leagueRepository.GetMatchesForSeasonAsync(seasonId);
            var nameCache = await BuildRegistrationNameCacheAsync(seasonId);

            var items = matches.Select(m => new LeagueMatchListItemViewModel
            {
                Match = m,
                GameTitle = season.Game,
                Team1Name = ResolveName(nameCache, m.HomeRegistrationId),
                Team2Name = ResolveName(nameCache, m.AwayRegistrationId),
                ScheduleLabel = m.ScheduledTime.ToString("dd MMM yyyy, HH:mm")
            }).ToList();

            return new LeagueMatchesViewModel { Season = season, Matches = items };
        }

        public async Task<LeagueBracketViewModel?> GetPlayoffBracketAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return null;
            var matches = (await _leagueRepository.GetMatchesForSeasonAsync(seasonId))
                .Where(m => m.IsPlayoff)
                .ToList();
            var nameCache = await BuildRegistrationNameCacheAsync(seasonId);

            var rounds = new List<List<MatchInfoDomain>>();
            foreach (var grp in matches.GroupBy(m => m.Round ?? 1).OrderBy(g => g.Key))
            {
                int matchNum = 1;
                var list = new List<MatchInfoDomain>();
                foreach (var m in grp.OrderBy(m => m.Id))
                {
                    list.Add(new MatchInfoDomain
                    {
                        Team1Name = ResolveName(nameCache, m.HomeRegistrationId),
                        Team2Name = m.AwayRegistrationId.HasValue ? ResolveName(nameCache, m.AwayRegistrationId) : null,
                        Team1Score = m.IsPlayed ? m.HomeScore : (int?)null,
                        Team2Score = m.IsPlayed && m.AwayRegistrationId.HasValue ? m.AwayScore : (int?)null,
                        Round = grp.Key,
                        MatchNum = matchNum++
                    });
                }
                rounds.Add(list);
            }
            return new LeagueBracketViewModel { Season = season, Rounds = rounds };
        }

        public async Task<LeagueParticipantsViewModel?> GetParticipantsAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return null;
            var regs = await _leagueRepository.GetRegistrationsForSeasonAsync(seasonId);
            var teams = new List<LeagueParticipantTeamViewModel>();
            foreach (var reg in regs)
            {
                var memberIds = await _teamsRepository.GetMemberIdsAsync(reg.LeagueTeamId);
                var names = new List<string>();
                foreach (var id in memberIds)
                {
                    var user = await _userRepository.FindByIdAsync(id);
                    if (user != null) names.Add(user.DisplayName ?? user.UserName ?? id);
                }
                teams.Add(new LeagueParticipantTeamViewModel
                {
                    TeamId = reg.LeagueTeamId,
                    Name = await ResolveRegistrationNameAsync(reg),
                    FeePaid = reg.FeePaid,
                    Members = names
                });
            }
            return new LeagueParticipantsViewModel { Season = season, Teams = teams };
        }

        public async Task<LeagueRulesViewModel?> GetRulesAsync(int seasonId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return null;
            var rules = !string.IsNullOrWhiteSpace(season.Rules)
                ? season.Rules.Split('\n').Select(r => r.Trim()).Where(r => r.Length > 0).ToList()
                : GameCatalog.DefaultRules(season.Game);
            return new LeagueRulesViewModel { Season = season, Rules = rules };
        }

        private async Task<Dictionary<int, string>> BuildRegistrationNameCacheAsync(int seasonId)
        {
            var regs = await _leagueRepository.GetRegistrationsForSeasonAsync(seasonId, includeRemoved: true);
            var cache = new Dictionary<int, string>();
            foreach (var reg in regs)
            {
                cache[reg.Id] = await ResolveRegistrationNameAsync(reg);
            }
            return cache;
        }

        private static string ResolveName(Dictionary<int, string> cache, int? registrationId)
        {
            if (!registrationId.HasValue) return "BYE";
            return cache.TryGetValue(registrationId.Value, out var name) ? name : "[?]";
        }
        public async Task<LeagueScheduleViewModel?> GetScheduleAsync(int matchId, string userId)
        {
            var match = await _leagueRepository.GetMatchByIdAsync(matchId);
            if (match == null) return null;
            var season = await _leagueRepository.GetSeasonByIdAsync(match.SeasonId);
            if (season == null) return null;
            var nameCache = await BuildRegistrationNameCacheAsync(match.SeasonId);

            bool canPropose = !match.IsPlayed
                && (await UserOwnsRegistrationAsync(match.HomeRegistrationId, userId)
                    || await UserOwnsRegistrationAsync(match.AwayRegistrationId, userId));

            return new LeagueScheduleViewModel
            {
                Season = season,
                Match = match,
                Team1Name = ResolveName(nameCache, match.HomeRegistrationId),
                Team2Name = ResolveName(nameCache, match.AwayRegistrationId),
                CanPropose = canPropose
            };
        }

        public async Task ProposeScheduleAsync(int matchId, string userId, DateTime proposedTime, string? server)
        {
            var match = await _leagueRepository.GetMatchByIdAsync(matchId);
            if (match == null || match.IsPlayed) return;
            int? proposerReg = null;
            if (await UserOwnsRegistrationAsync(match.HomeRegistrationId, userId)) proposerReg = match.HomeRegistrationId;
            else if (await UserOwnsRegistrationAsync(match.AwayRegistrationId, userId)) proposerReg = match.AwayRegistrationId;
            if (proposerReg == null) return;

            match.ProposedTime = proposedTime;
            match.ProposedByRegistrationId = proposerReg;
            match.Server = server;
            match.ScheduledTime = proposedTime;
            await _leagueRepository.UpdateMatchAsync(match);
        }

        private async Task<bool> UserOwnsRegistrationAsync(int? registrationId, string userId)
        {
            if (!registrationId.HasValue || string.IsNullOrEmpty(userId)) return false;
            var reg = await _leagueRepository.GetRegistrationByIdAsync(registrationId.Value);
            if (reg == null) return false;
            return await _teamsRepository.IsMemberAsync(reg.LeagueTeamId, userId);
        }

        public async Task<LeagueChatViewModel?> GetChatAsync(int seasonId, string? userId, bool isAdmin)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return null;

            int? userTeamId = await GetUserSeasonTeamIdAsync(seasonId, userId);
            var model = new LeagueChatViewModel
            {
                Season = season,
                HasTeam = userTeamId != null,
                UserTeamId = userTeamId,
                AllMessages = await _leagueRepository.GetAllChatMessagesAsync(seasonId)
            };
            if (userTeamId != null)
            {
                model.TeamMessages = await _leagueRepository.GetTeamChatMessagesAsync(seasonId, userTeamId.Value);
            }
            model.ReportMessages = await _leagueRepository.GetReportChatMessagesAsync(seasonId, userTeamId, isAdmin);

            var ids = model.AllMessages.Select(m => m.UserId)
                .Concat(model.TeamMessages.Select(m => m.UserId))
                .Concat(model.ReportMessages.Select(m => m.UserId))
                .Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            foreach (var id in ids)
            {
                var user = await _userRepository.FindByIdAsync(id);
                if (user != null && !model.UserNames.ContainsKey(id))
                {
                    model.UserNames[id] = user.UserName ?? id;
                }
            }
            return model;
        }

        public async Task PostMessageAsync(int seasonId, string userId, string message, bool toTeam)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            int? teamId = null;
            if (toTeam)
            {
                teamId = await GetUserSeasonTeamIdAsync(seasonId, userId);
                if (teamId == null) return;
            }
            await _leagueRepository.AddChatMessageAsync(new ChatMessageDomain
            {
                SeasonId = seasonId,
                TeamId = teamId,
                UserId = userId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsReport = false
            });
        }

        public async Task PostReportMessageAsync(int seasonId, string userId, string message, int? teamId, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            int? finalTeamId;
            if (isAdmin)
            {
                if (!teamId.HasValue) return;
                finalTeamId = teamId;
            }
            else
            {
                finalTeamId = await GetUserSeasonTeamIdAsync(seasonId, userId);
                if (finalTeamId == null) return;
            }
            await _leagueRepository.AddChatMessageAsync(new ChatMessageDomain
            {
                SeasonId = seasonId,
                TeamId = finalTeamId,
                UserId = userId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsReport = true
            });
        }

        private async Task<int?> GetUserSeasonTeamIdAsync(int seasonId, string? userId)
        {
            if (string.IsNullOrEmpty(userId)) return null;
            var regs = await _leagueRepository.GetRegistrationsForSeasonAsync(seasonId);
            foreach (var reg in regs)
            {
                if (await _teamsRepository.IsMemberAsync(reg.LeagueTeamId, userId))
                {
                    return reg.LeagueTeamId;
                }
            }
            return null;
        }

        public async Task<LeagueMatchDetailsViewModel?> GetMatchDetailsAsync(int seasonId, int matchId)
        {
            var season = await _leagueRepository.GetSeasonByIdAsync(seasonId);
            if (season == null) return null;
            var match = await _leagueRepository.GetMatchByIdAsync(matchId);
            if (match == null || match.SeasonId != seasonId) return null;

            var nameCache = await BuildRegistrationNameCacheAsync(seasonId);
            var vm = new LeagueMatchDetailsViewModel
            {
                Season = season,
                Match = match,
                GameTitle = season.Game,
                Team1Name = ResolveName(nameCache, match.HomeRegistrationId),
                Team2Name = ResolveName(nameCache, match.AwayRegistrationId)
            };

            var homeMemberIds = await GetRosterAsync(match.HomeRegistrationId);
            vm.Team1ProfileUrl = await CaptainAvatarAsync(match.HomeRegistrationId);
            vm.Team2ProfileUrl = await CaptainAvatarAsync(match.AwayRegistrationId);

            var stats = await _leagueRepository.GetPlayerStatsForLeagueMatchAsync(matchId);
            foreach (var stat in stats)
            {
                var user = await _userRepository.FindByIdAsync(stat.UserId);
                if (user == null) continue;
                var row = new PlayerStatViewModel
                {
                    Player = user,
                    Kills = stat.Kills,
                    Deaths = stat.Deaths,
                    Assists = stat.Assists,
                    Champion = stat.Champion
                };
                if (homeMemberIds.Contains(stat.UserId)) vm.Team1Stats.Add(row);
                else vm.Team2Stats.Add(row);
            }
            return vm;
        }

        private async Task<List<string>> GetRosterAsync(int? registrationId)
        {
            if (!registrationId.HasValue) return new List<string>();
            var reg = await _leagueRepository.GetRegistrationByIdAsync(registrationId.Value);
            if (reg == null) return new List<string>();
            return await _teamsRepository.GetMemberIdsAsync(reg.LeagueTeamId);
        }

        private async Task<string> CaptainAvatarAsync(int? registrationId)
        {
            const string fallback = "/images/default-profile.png";
            if (!registrationId.HasValue) return fallback;
            var reg = await _leagueRepository.GetRegistrationByIdAsync(registrationId.Value);
            var captainId = reg?.LeagueTeam?.CaptainId;
            if (string.IsNullOrEmpty(captainId)) return fallback;
            var captain = await _userRepository.FindByIdAsync(captainId);
            return string.IsNullOrEmpty(captain?.ProfilePicturePath) ? fallback : captain!.ProfilePicturePath!;
        }
    }
}
