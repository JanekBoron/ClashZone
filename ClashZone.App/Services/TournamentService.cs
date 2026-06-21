using ClashZone.App.Domains;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Services.Interfaces;
using ClashZone.App.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClashZone.App.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        public TournamentService(
            ITournamentsRepository tournamentsRepository,
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository)
        {
            _tournamentsRepository = tournamentsRepository;
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
        }

        public async Task<List<TournamentDomain>> GetUpcomingTournamentsAsync(string? format)
        {
            return (List<TournamentDomain>)await _tournamentsRepository.GetUpcomingTournamentsAsync(format);
        }

        public async Task<List<TournamentDomain>> GetUserTournamentsAsync(string userId)
        {
            return (List<TournamentDomain>)await _tournamentsRepository.GetUserTournamentsAsync(userId);
        }
        public async Task CreateTournamentAsync(TournamentDomain tournament, string createdByUserId)
        {
            if (tournament == null) throw new ArgumentNullException(nameof(tournament));
            tournament.CreatedByUserId = createdByUserId;
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
            var viewModel = new TournamentDetailsViewModel
            {
                Tournament = tournament,
                CurrentUserId = userId,
                RequiredRosterSize = GameCatalog.RosterSize(tournament.Format)
            };

            var confirmedCount = await _tournamentsRepository.GetConfirmedTeamCountAsync(id);
            viewModel.TournamentFull = tournament.MaxParticipants > 0 && confirmedCount >= tournament.MaxParticipants;

            if (!string.IsNullOrEmpty(userId))
            {
                var team = await _tournamentsRepository.GetUserTeamAsync(id, userId);
                if (team != null)
                {
                    viewModel.UserTeam = team;
                    viewModel.IsCaptain = team.CaptainId == userId;
                    viewModel.IsTeamConfirmed = team.IsConfirmed;

                    var members = await _tournamentsRepository.GetTeamMembersAsync(team.Id);
                    foreach (var m in members)
                    {
                        var user = await _userRepository.FindByIdAsync(m.UserId);
                        viewModel.Members.Add(new TournamentTeamMemberView
                        {
                            UserId = m.UserId,
                            Name = user?.DisplayName ?? user?.UserName ?? m.UserId,
                            Ready = m.IsReady
                        });
                    }

                    viewModel.CanConfirm = viewModel.IsCaptain
                        && !viewModel.IsTeamConfirmed
                        && !viewModel.TournamentFull
                        && viewModel.Members.Count == viewModel.RequiredRosterSize
                        && viewModel.Members.All(x => x.Ready);
                }
            }
            return viewModel;
        }

        public async Task<TeamActionResult> ToggleReadyAsync(int tournamentId, string userId)
        {
            var team = await _tournamentsRepository.GetUserTeamAsync(tournamentId, userId);
            if (team == null)
            {
                return new TeamActionResult { Success = false, Message = "Nie należysz do żadnej drużyny w tym turnieju." };
            }

            var members = await _tournamentsRepository.GetTeamMembersAsync(team.Id);
            var me = members.FirstOrDefault(m => m.UserId == userId);
            var newReady = !(me?.IsReady ?? false);

            if (newReady && !team.IsConfirmed)
            {
                var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
                var confirmedCount = await _tournamentsRepository.GetConfirmedTeamCountAsync(tournamentId);
                if (tournament != null && tournament.MaxParticipants > 0 && confirmedCount >= tournament.MaxParticipants)
                {
                    return new TeamActionResult { Success = false, Message = "Turniej osiągnął limit drużyn - nie można ustawić gotowości." };
                }
            }

            await _tournamentsRepository.SetMemberReadyAsync(team.Id, userId, newReady);
            return new TeamActionResult { Success = true, Message = newReady ? "Oznaczono gotowość." : "Cofnięto gotowość." };
        }

        public async Task<TeamActionResult> ConfirmTeamAsync(int tournamentId, string userId)
        {
            var team = await _tournamentsRepository.GetUserTeamAsync(tournamentId, userId);
            if (team == null)
            {
                return new TeamActionResult { Success = false, Message = "Nie należysz do żadnej drużyny w tym turnieju." };
            }
            if (team.CaptainId != userId)
            {
                return new TeamActionResult { Success = false, Message = "Tylko kapitan może potwierdzić drużynę." };
            }
            if (team.IsConfirmed)
            {
                return new TeamActionResult { Success = true, Message = "Drużyna jest już potwierdzona." };
            }

            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return new TeamActionResult { Success = false, Message = "Nie znaleziono turnieju." };
            }

            var confirmedCount = await _tournamentsRepository.GetConfirmedTeamCountAsync(tournamentId);
            if (tournament.MaxParticipants > 0 && confirmedCount >= tournament.MaxParticipants)
            {
                return new TeamActionResult { Success = false, Message = "Turniej osiągnął limit potwierdzonych drużyn." };
            }

            var required = GameCatalog.RosterSize(tournament.Format);
            var members = await _tournamentsRepository.GetTeamMembersAsync(team.Id);
            if (members.Count != required)
            {
                return new TeamActionResult { Success = false, Message = $"Skład musi liczyć dokładnie {required} graczy (obecnie {members.Count})." };
            }
            if (!members.All(m => m.IsReady))
            {
                return new TeamActionResult { Success = false, Message = "Wszyscy gracze muszą być gotowi przed potwierdzeniem." };
            }

            await _tournamentsRepository.ConfirmTeamAsync(team.Id);
            return new TeamActionResult { Success = true, Message = "Drużyna potwierdzona - wchodzi do drabinki." };
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
            var existingTeams = await _tournamentsRepository.GetTeamsForTournamentAsync(id);
            if (tournament.MaxParticipants > 0 && existingTeams != null && existingTeams.Count >= tournament.MaxParticipants)
            {
                result.MaxParticipantsExceeded = true;
                return result;
            }
            if (tournament.IsPremium)
            {
                var subscription = await _subscriptionRepository.GetActiveSubscriptionAsync(userId);
                if (subscription == null || !subscription.Plan.IsPremiumAccess)
                {
                    result.RequiresSubscription = true;
                    return result;
                }
            }
            var existingTeam = await _tournamentsRepository.GetUserTeamAsync(id, userId);
            if (existingTeam != null)
            {
                result.AlreadyJoined = true;
                return result;
            }
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