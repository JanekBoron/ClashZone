using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClashZone.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly UserManager<ClashUser> _userManager;
        public TournamentService(
            ITournamentsRepository tournamentsRepository,
            ISubscriptionRepository subscriptionRepository,
            UserManager<ClashUser> userManager)
        {
            _tournamentsRepository = tournamentsRepository;
            _subscriptionRepository = subscriptionRepository;
            _userManager = userManager;
        }

        public async Task<List<Tournament>> GetUpcomingTournamentsAsync(string? format)
        {
            return (List<Tournament>)await _tournamentsRepository.GetUpcomingTournamentsAsync(format);
        }

        public async Task<List<Tournament>> GetUserTournamentsAsync(string userId)
        {
            return (List<Tournament>)await _tournamentsRepository.GetUserTournamentsAsync(userId);
        }
        public async Task CreateTournamentAsync(Tournament tournament, string createdByUserId)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            tournament.CreatedByUserId = createdByUserId;
            // Generate join code for private tournaments if not already set
            if (!tournament.IsPublic && string.IsNullOrEmpty(tournament.JoinCode))
            {
                tournament.JoinCode = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            }
            await _tournamentsRepository.AddTournamentAsync(tournament);
        }

        public async Task<TournamentDetailsViewModel?> GetTournamentDetailsAsync(int id, string? userId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                return null;
            }
            var viewModel = new TournamentDetailsViewModel { Tournament = tournament };
            if (!string.IsNullOrEmpty(userId))
            {
                var team = await _tournamentsRepository.GetUserTeamAsync(id, userId);
                if (team != null)
                {
                    viewModel.UserTeam = team;
                    var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
                    foreach (var uid in memberIds)
                    {
                        var user = await _userManager.FindByIdAsync(uid);
                        if (user != null)
                        {
                            viewModel.TeamMembers.Add(user.UserName);
                        }
                    }
                }
            }
            return viewModel;
        }

        public async Task<JoinTournamentResult> JoinTournamentAsync(int id, string userId)
        {
            var result = new JoinTournamentResult { TournamentId = id };
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(id);
            if (tournament == null)
            {
                result.NotFound = true;
                return result;
            }
            // Check if the tournament has reached its maximum number of participants
            var existingTeams = await _tournamentsRepository.GetTeamsForTournamentAsync(id);
            if (tournament.MaxParticipants > 0 && existingTeams != null && existingTeams.Count >= tournament.MaxParticipants)
            {
                result.MaxParticipantsExceeded = true;
                return result;
            }
            // Check premium requirement
            if (tournament.IsPremium)
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
                if (subscription == null || !subscription.Plan.IsPremiumAccess)
                {
                    result.RequiresSubscription = true;
                    return result;
                }
            }
            // Check if already joined
            var existingTeam = await _tournamentsRepository.GetUserTeamAsync(id, userId);
            if (existingTeam != null)
            {
                result.AlreadyJoined = true;
                return result;
            }
            // Create new team with current user as captain
            var team = await _tournamentsRepository.CreateTeamWithCaptainAsync(id, userId);
            result.Team = team;
            result.TournamentFormat = tournament.Format;
            result.IsPremium = tournament.IsPremium;
            return result;
        }

        public async Task<int?> JoinTeamAsync(int teamId, string userId, string code)
        {
            return await _tournamentsRepository.AddUserToTeamAsync(teamId, userId, code);
        }

    }
}