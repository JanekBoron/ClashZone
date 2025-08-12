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

        public async Task<ChatViewModel?> GetChatAsync(int tournamentId, string? userId)
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
                HasTeam = userTeam != null
            };
            model.AllMessages = await _tournamentsRepository.GetAllChatMessagesAsync(tournamentId);
            if (userTeam != null)
            {
                model.TeamMessages = await _tournamentsRepository.GetTeamChatMessagesAsync(tournamentId, userTeam.Id);
            }
            // Build a dictionary of user IDs to display names to avoid repeated lookups in the view
            var allUserIds = model.AllMessages.Select(m => m.UserId)
                .Concat(model.TeamMessages.Select(m => m.UserId))
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
                SentAt = DateTime.UtcNow
            };
            await _tournamentsRepository.AddChatMessageAsync(chatMessage);
        }
    }
}
