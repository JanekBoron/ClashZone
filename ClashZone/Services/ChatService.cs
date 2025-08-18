using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace ClashZone.Services
{
    public class ChatService : IChatService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly UserManager<ClashUser> _userManager;

        public ChatService(ITournamentsRepository tournamentsRepository, UserManager<ClashUser> userManager)
        {
            _tournamentsRepository = tournamentsRepository;
            _userManager = userManager;
        }

        public async Task<ChatViewModel?> GetChatAsync(int tournamentId, string? userId, bool isAdmin)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            // Determine whether the user has a team in this tournament
            Team? userTeam = null;
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
            // Load all‑chat messages
            model.AllMessages = await _tournamentsRepository.GetAllChatMessagesAsync(tournamentId);
            // Load team‑chat messages if user belongs to a team
            if (userTeam != null)
            {
                model.TeamMessages = await _tournamentsRepository.GetTeamChatMessagesAsync(tournamentId, userTeam.Id);
            }
            // Load problem report messages.  Administrators see all reports; players see only their own team's reports
            model.ReportMessages = await _tournamentsRepository.GetReportChatMessagesAsync(tournamentId, userTeam?.Id, isAdmin);
            // Build a dictionary of user IDs to display names to avoid repeated lookups in the view
            var allUserIds = model.AllMessages.Select(m => m.UserId)
                .Concat(model.TeamMessages.Select(m => m.UserId))
                .Concat(model.ReportMessages.Select(m => m.UserId))
                .Distinct()
                .Where(id => id != null)
                .ToList();
            foreach (var uid in allUserIds)
            {
                var user = await _userManager.FindByIdAsync(uid);
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
                    // User attempted to post to team chat but is not a member; skip posting.
                    return;
                }
                teamId = team.Id;
            }
            var chatMessage = new ChatMessage
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
                // Admin can specify a team id to post to.
                if (teamId.HasValue)
                {
                    finalTeamId = teamId;
                }
                else
                {
                    // Without a team target the report cannot be routed
                    return;
                }
            }
            else
            {
                var team = await _tournamentsRepository.GetUserTeamAsync(tournamentId, userId);
                if (team == null)
                {
                    // Only team members can post reports
                    return;
                }
                finalTeamId = team.Id;
            }
            var chatMessage = new ChatMessage
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
