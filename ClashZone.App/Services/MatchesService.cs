using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    public class MatchesService : IMatchesService
    {
        private readonly IMatchesRepository _matchesRepository;
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly IUserRepository _userRepository;

        public MatchesService(
            IMatchesRepository matchesRepository,
            ITournamentsRepository tournamentsRepository,
            IUserRepository userRepository)
        {
            _matchesRepository = matchesRepository;
            _tournamentsRepository = tournamentsRepository;
            _userRepository = userRepository;
        }

        /// <inheritdoc />
        public async Task<List<MatchListItemViewModel>> GetMatchesForTournamentAsync(int tournamentId)
        {
            var matches = await _matchesRepository.GetMatchesByTournamentAsync(tournamentId);
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            var gameTitle = tournament?.GameTitle ?? string.Empty;
            var items = new List<MatchListItemViewModel>();
            foreach (var match in matches)
            {
                // Resolve team entities
                var team1 = match.Team1Id.HasValue ? await _matchesRepository.GetTeamByIdAsync(match.Team1Id.Value) : null;
                var team2 = match.Team2Id.HasValue ? await _matchesRepository.GetTeamByIdAsync(match.Team2Id.Value) : null;

                string team1Name;
                string team2Name;

                if (!match.Team1Id.HasValue)
                {
                    team1Name = "BYE";
                }
                else if (team1 == null)
                {
                    team1Name = "[TEAM NOT FOUND]";
                }
                else if (!string.IsNullOrWhiteSpace(team1.Name))
                {
                    team1Name = team1.Name;
                }
                else
                {
                    var captain = await _userRepository.FindByIdAsync(team1.CaptainId);
                    team1Name = captain?.UserName ?? "[UNNAMED TEAM]";
                }

                if (!match.Team2Id.HasValue)
                {
                    team2Name = "BYE";
                }
                else if (team2 == null)
                {
                    team2Name = "[TEAM NOT FOUND]";
                }
                else if (!string.IsNullOrWhiteSpace(team2.Name))
                {
                    team2Name = team2.Name;
                }
                else
                {
                    var captain = await _userRepository.FindByIdAsync(team2.CaptainId);
                    team2Name = captain?.UserName ?? "[UNNAMED TEAM]";
                }

                // Resolve captain avatars
                string team1Profile = string.Empty;
                string team2Profile = string.Empty;
                if (team1 != null)
                {
                    var captain = await _userRepository.FindByIdAsync(team1.CaptainId);
                    if (captain != null && !string.IsNullOrEmpty(captain.ProfilePicturePath))
                    {
                        team1Profile = captain.ProfilePicturePath;
                    }
                }
                if (team2 != null)
                {
                    var captain = await _userRepository.FindByIdAsync(team2.CaptainId);
                    if (captain != null && !string.IsNullOrEmpty(captain.ProfilePicturePath))
                    {
                        team2Profile = captain.ProfilePicturePath;
                    }
                }
                // Fallback to default images if no custom avatar exists
                if (string.IsNullOrEmpty(team1Profile))
                {
                    team1Profile = "/images/default-profile.png";
                }
                if (string.IsNullOrEmpty(team2Profile))
                {
                    team2Profile = "/images/default-profile.png";
                }

                items.Add(new MatchListItemViewModel
                {
                    Match = match,
                    GameTitle = gameTitle,
                    Team1Name = team1Name,
                    Team2Name = team2Name,
                    Team1ProfileUrl = team1Profile,
                    Team2ProfileUrl = team2Profile
                });
            }
            return items;
        }

        /// <inheritdoc />
        public async Task<MatchDetailsViewModel?> GetMatchDetailsAsync(int tournamentId, int matchId)
        {
            var match = await _matchesRepository.GetMatchByIdAsync(matchId, tournamentId);
            if (match == null)
            {
                return null;
            }
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            var gameTitle = tournament?.GameTitle ?? string.Empty;
            // Load teams
            var team1 = match.Team1Id.HasValue ? await _matchesRepository.GetTeamByIdAsync(match.Team1Id.Value) : null;
            var team2 = match.Team2Id.HasValue ? await _matchesRepository.GetTeamByIdAsync(match.Team2Id.Value) : null;

            string team1Name;
            string team2Name;

            if (!match.Team1Id.HasValue)
            {
                team1Name = "BYE";
            }
            else if (team1 == null)
            {
                team1Name = "[TEAM NOT FOUND]";
            }
            else if (!string.IsNullOrWhiteSpace(team1.Name))
            {
                team1Name = team1.Name;
            }
            else
            {
                var captain = await _userRepository.FindByIdAsync(team1.CaptainId);
                team1Name = captain?.UserName ?? "[UNNAMED TEAM]";
            }

            if (!match.Team2Id.HasValue)
            {
                team2Name = "BYE";
            }
            else if (team2 == null)
            {
                team2Name = "[TEAM NOT FOUND]";
            }
            else if (!string.IsNullOrWhiteSpace(team2.Name))
            {
                team2Name = team2.Name;
            }
            else
            {
                var captain = await _userRepository.FindByIdAsync(team2.CaptainId);
                team2Name = captain?.UserName ?? "[UNNAMED TEAM]";
            }
            string team1Profile = "/images/default-profile.png";
            string team2Profile = "/images/default-profile.png";
            if (team1 != null)
            {
                var captain = await _userRepository.FindByIdAsync(team1.CaptainId);
                if (captain != null && !string.IsNullOrEmpty(captain.ProfilePicturePath))
                {
                    team1Profile = captain.ProfilePicturePath;
                }
            }
            if (team2 != null)
            {
                var captain = await _userRepository.FindByIdAsync(team2.CaptainId);
                if (captain != null && !string.IsNullOrEmpty(captain.ProfilePicturePath))
                {
                    team2Profile = captain.ProfilePicturePath;
                }
            }

            // Load per-player stats
            var stats = await _matchesRepository.GetPlayerStatsForMatchAsync(match.Id);
            var team1Stats = new List<PlayerStatViewModel>();
            var team2Stats = new List<PlayerStatViewModel>();
            // Determine team membership lists for each team; if team1 exists gather member IDs
            var team1MemberIds = new List<string>();
            if (team1 != null)
            {
                team1MemberIds = (await _tournamentsRepository.GetTeamMemberIdsAsync(team1.Id)).ToList();
            }
            // Build view models per player
            foreach (var stat in stats)
            {
                var user = await _userRepository.FindByIdAsync(stat.UserId);
                if (user == null) continue;
                var vm = new PlayerStatViewModel
                {
                    Player = user,
                    Kills = stat.Kills,
                    Deaths = stat.Deaths,
                    Assists = stat.Assists,
                    Champion = stat.Champion
                };
                // Assign to appropriate team based on membership; if membership cannot be resolved, default to team2
                if (team1MemberIds.Contains(user.Id))
                {
                    team1Stats.Add(vm);
                }
                else
                {
                    team2Stats.Add(vm);
                }
            }
            var model = new MatchDetailsViewModel
            {
                Match = match,
                GameTitle = gameTitle,
                Team1Stats = team1Stats,
                Team2Stats = team2Stats,
                Team1Name = team1Name,
                Team2Name = team2Name,
                Team1ProfileUrl = team1Profile,
                Team2ProfileUrl = team2Profile
            };
            return model;
        }
    }
}