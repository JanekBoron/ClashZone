using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.Models;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    /// <summary>
    /// Contract for retrieving tournaments from the database.  It includes
    /// methods for listing upcoming tournaments with optional filtering and
    /// retrieving tournaments for a specific user.
    /// </summary>
    public interface ITournamentsRepository
    {
        /// <summary>
        /// Pobiera listę nadchodzących turniejów.  Jeśli parametr format nie
        /// jest pusty, wyniki zostaną ograniczone do określonego rozmiaru drużyn
        /// (np. "1v1", "2v2", "5v5").
        /// </summary>
        Task<IEnumerable<Tournament>> GetUpcomingTournamentsAsync(string? format);

        /// <summary>
        /// Pobiera listę turniejów, w których uczestniczy lub zapisał się dany
        /// użytkownik.  Implementacja powinna opierać się na tabeli
        /// pośredniczącej (np. UserTournaments).  Jeśli taka tabela nie
        /// istnieje, jako alternatywa można zwrócić turnieje utworzone przez
        /// użytkownika (CreatedByUserId).
        /// </summary>
        Task<IEnumerable<Tournament>> GetUserTournamentsAsync(string userId);
    }
}
