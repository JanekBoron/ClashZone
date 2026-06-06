using System.Linq;
using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    /// <summary>
    /// Logika trwałych drużyn ligowych (Team Management): tworzenie drużyn,
    /// dołączanie przez link/kod z egzekwowaniem okresów karencji.
    /// </summary>
    public class LeagueTeamService : ILeagueTeamService
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromDays(7);
        private const int MaxJoinsPerRosterPerWeek = 3;

        private readonly ILeagueTeamsRepository _teamsRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITournamentsRepository _tournamentsRepository;

        public LeagueTeamService(
            ILeagueTeamsRepository teamsRepository,
            IUserRepository userRepository,
            ITournamentsRepository tournamentsRepository)
        {
            _teamsRepository = teamsRepository;
            _userRepository = userRepository;
            _tournamentsRepository = tournamentsRepository;
        }

        public async Task<LeagueTeamDomain> CreateTeamAsync(string name, string game, string captainId)
        {
            var team = new LeagueTeamDomain
            {
                Name = string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
                Game = GameCatalog.Normalize(game) ?? game,
                CaptainId = captainId,
                JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper(),
                CreatedAt = DateTime.UtcNow
            };
            return await _teamsRepository.CreateTeamAsync(team);
        }

        public async Task<TeamManagementViewModel> GetUserTeamsAsync(string userId)
        {
            // Pokazujemy tylko drużyny ZAŁOŻONE przez użytkownika (kapitan), nie te,
            // do których jedynie dołączył — osobno ligowe i turniejowe.
            var leagueTeams = await _teamsRepository.GetUserTeamsAsync(userId);
            var ownedLeagueTeams = leagueTeams.Where(t => t.CaptainId == userId).ToList();

            var captainTournamentTeams = await _tournamentsRepository.GetCaptainTeamsAsync(userId);
            var tournamentItems = new List<TournamentTeamListItem>();
            foreach (var team in captainTournamentTeams)
            {
                var tournament = await _tournamentsRepository.GetTournamentByIdAsync(team.TournamentId);
                tournamentItems.Add(new TournamentTeamListItem
                {
                    TeamId = team.Id,
                    TeamName = string.IsNullOrWhiteSpace(team.Name) ? $"Drużyna #{team.Id}" : team.Name!,
                    TournamentId = team.TournamentId,
                    TournamentName = tournament?.Name ?? $"Turniej #{team.TournamentId}",
                    Game = tournament?.GameTitle ?? string.Empty,
                    IsConfirmed = team.IsConfirmed
                });
            }

            return new TeamManagementViewModel
            {
                Teams = ownedLeagueTeams,
                TournamentTeams = tournamentItems
            };
        }

        public async Task<LeagueTeamDetailsViewModel?> GetTeamDetailsAsync(int teamId, string userId)
        {
            var team = await _teamsRepository.GetByIdAsync(teamId);
            if (team == null) return null;

            var memberIds = await _teamsRepository.GetMemberIdsAsync(teamId);
            var names = new List<string>();
            foreach (var id in memberIds)
            {
                var user = await _userRepository.FindByIdAsync(id);
                if (user != null)
                {
                    names.Add(user.DisplayName ?? user.UserName ?? id);
                }
            }

            return new LeagueTeamDetailsViewModel
            {
                Team = team,
                MemberNames = names,
                IsCaptain = team.CaptainId == userId,
                IsMember = memberIds.Contains(userId)
            };
        }

        public async Task<JoinLeagueTeamResult> JoinByCodeAsync(int teamId, string code, string userId)
        {
            var result = new JoinLeagueTeamResult { TeamId = teamId };
            var team = await _teamsRepository.GetByIdAsync(teamId);
            if (team == null)
            {
                result.NotFound = true;
                return result;
            }
            if (!string.Equals(team.JoinCode, code, StringComparison.OrdinalIgnoreCase))
            {
                result.InvalidCode = true;
                return result;
            }
            if (await _teamsRepository.IsMemberAsync(teamId, userId))
            {
                result.AlreadyMember = true;
                return result;
            }

            var since = DateTime.UtcNow - Cooldown;
            // Gracz może dołączyć tylko do jednego zespołu na 7 dni.
            if (await _teamsRepository.UserJoinedAnyTeamSinceAsync(userId, since))
            {
                result.UserCooldown = true;
                return result;
            }
            // Maksymalnie 3 graczy może dołączyć do rosteru na 7 dni.
            if (await _teamsRepository.CountMembersJoinedSinceAsync(teamId, since) >= MaxJoinsPerRosterPerWeek)
            {
                result.RosterCooldown = true;
                return result;
            }

            await _teamsRepository.AddMemberAsync(teamId, userId);
            result.Success = true;
            return result;
        }
    }
}
