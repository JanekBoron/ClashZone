using ClashZone.DataAccess.Models;
using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    public interface ITournamentsRepository
    {
        Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync(string? format);

        Task<IEnumerable<Tournament>> GetUserTournamentsAsync(string userId);

        Task AddTournamentAsync(Tournament tournament);

        Task<Tournament?> GetTournamentByIdAsync(int id);

        // ---------------------------------------------------------------------
        // Team management methods.  These methods encapsulate operations
        // related to creating and joining teams for tournaments.  They hide
        // direct access to the database context from the controller.

        Task<Team?> GetUserTeamAsync(int tournamentId, string userId);

        Task<List<string>> GetTeamMemberIdsAsync(int teamId);

        Task<Team> CreateTeamWithCaptainAsync(int tournamentId, string userId);

        Task<int?> AddUserToTeamAsync(int teamId, string userId, string code);

        // ---------------------------------------------------------------------
        // Chat related methods

        Task AddChatMessageAsync(ChatMessage message);

        Task<List<ChatMessage>> GetAllChatMessagesAsync(int tournamentId);

        Task<List<ChatMessage>> GetTeamChatMessagesAsync(int tournamentId, int teamId);

        Task<List<Team>> GetTeamsForTournamentAsync(int tournamentId);
    }
}
