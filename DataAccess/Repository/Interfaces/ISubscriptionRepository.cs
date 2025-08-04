using ClashZone.DataAccess.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClashZone.DataAccess.Repository.Interfaces
{
    /// <summary>
    /// Interfejs repozytorium subskrypcji.  Definiuje operacje umożliwiające
    /// pobieranie dostępnych planów, sprawdzanie aktywnej subskrypcji
    /// użytkownika oraz tworzenie nowej subskrypcji.  Dzięki zastosowaniu
    /// interfejsu logika dostępu do danych jest łatwa do podmiany lub
    /// przetestowania.
    /// </summary>
    public interface ISubscriptionRepository
    {
        /// <summary>
        /// Zwraca wszystkie dostępne plany subskrypcji.
        /// </summary>
        Task<SubscriptionPlan[]> GetAllPlansAsync();

        /// <summary>
        /// Pobiera aktywną subskrypcję użytkownika (jeśli istnieje).
        /// </summary>
        /// <param name="userId">Identyfikator użytkownika</param>
        /// <returns>Aktywna subskrypcja lub null jeśli brak</returns>
        Task<UserSubscription?> GetActiveSubscriptionAsync(string userId);

        /// <summary>
        /// Tworzy nową subskrypcję dla użytkownika na podstawie wybranego planu.
        /// Data wygaśnięcia jest liczona na podstawie DurationDays w planie.
        /// </summary>
        Task CreateSubscriptionAsync(string userId, int planId);
    }
}
