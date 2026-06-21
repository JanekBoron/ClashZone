using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;

namespace ClashZone.App.Services
{
    public class ChatService : IChatService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly IUserRepository _userRepository;

        public ChatService(ITournamentsRepository tournamentsRepository, IUserRepository userRepository)
        {
            _tournamentsRepository = tournamentsRepository;
            _userRepository = userRepository;
        }

        public async Task<ChatViewModel?> GetChatAsync(int tournamentId, string? userId, bool isAdmin)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            TeamDomain? userTeam = null;
            if (!string.IsNullOrEmpty(userId))
            {
                userTeam = await _tournamentsRepository.GetUserTeamAsync(tournamentId, userId);
            }

            var model = new ChatViewModel
            {
                Tournament = tournament,
                HasTeam = userTeam != null,
                UserTeamId = userTeam?.Id
            };
            model.AllMessages = await _tournamentsRepository.GetAllChatMessagesAsync(tournamentId);
            if (userTeam != null)
            {
                model.TeamMessages = await _tournamentsRepository.GetTeamChatMessagesAsync(tournamentId, userTeam.Id);
            }
            model.ReportMessages = await _tournamentsRepository.GetReportChatMessagesAsync(tournamentId, userTeam?.Id, isAdmin);
            var allUserIds = model.AllMessages.Select(m => m.UserId)
                .Concat(model.TeamMessages.Select(m => m.UserId))
                .Concat(model.ReportMessages.Select(m => m.UserId))
                .Distinct()
                .Where(id => id != null)
                .ToList();
            foreach (var uid in allUserIds)
            {
                var user = await _userRepository.FindByIdAsync(uid);
                if (user != null && !model.UserNames.ContainsKey(uid))
                {
                    model.UserNames[uid] = user.UserName;
                }
            }
            return model;
        }

        public async Task PostMessageAsync(int tournamentId, string userId, string message, bool toTeam)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            int? teamId = null;
            if (toTeam)
            {
                var team = await _tournamentsRepository.GetUserTeamAsync(tournamentId, userId);
                if (team == null)
                {
                    return;
                }
                teamId = team.Id;
            }
            var chatMessage = new ChatMessageDomain
            {
                TournamentId = tournamentId,
                TeamId = teamId,
                UserId = userId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsReport = false
            };
            await _tournamentsRepository.AddChatMessageAsync(chatMessage);
        }

        public async Task PostReportMessageAsync(int tournamentId, string userId, string message, int? teamId, bool isAdmin)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }
            int? finalTeamId = null;
            if (isAdmin)
            {
                if (teamId.HasValue)
                {
                    finalTeamId = teamId;
                }
                else
                {
                    return;
                }
            }
            else
            {
                var team = await _tournamentsRepository.GetUserTeamAsync(tournamentId, userId);
                if (team == null)
                {
                    return;
                }
                finalTeamId = team.Id;
            }
            var chatMessage = new ChatMessageDomain
            {
                TournamentId = tournamentId,
                TeamId = finalTeamId,
                UserId = userId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsReport = true
            };
            await _tournamentsRepository.AddChatMessageAsync(chatMessage);
        }
    }
}
