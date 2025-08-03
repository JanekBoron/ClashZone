using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Models;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    /// <summary>
    /// Contract for retrieving and manipulating tournament data.  In addition to
    /// listing upcoming tournaments and tournaments for a specific user, this
    /// interface exposes a method for retrieving a single tournament by its
    /// primary key.
    /// </summary>
    public interface ITournamentsRepository
    {
        /// <summary>
        /// Retrieves a list of upcoming tournaments.  If <paramref name="format"/>
        /// is not empty, the results are filtered by the specified team size
        /// (e.g. "1v1", "2v2", "5v5").
        /// </summary>
        Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync(string? format);

        /// <summary>
        /// Retrieves tournaments in which the given user participates or has
        /// registered.  If a join table is not implemented, implementations
        /// should return tournaments created by the user (<see
        /// cref="Tournament.CreatedByUserId"/>).
        /// </summary>
        Task<IEnumerable<Tournament>> GetUserTournamentsAsync(string userId);

        /// <summary>
        /// Adds a new tournament to the underlying data store.
        /// </summary>
        Task AddTournamentAsync(Tournament tournament);

        /// <summary>
        /// Retrieves a single tournament by its identifier.  Returns <c>null</c>
        /// if no tournament with the provided ID exists.
        /// </summary>
        /// <param name="id">Primary key of the tournament.</param>
        Task<Tournament?> GetTournamentByIdAsync(int id);
    }
}
