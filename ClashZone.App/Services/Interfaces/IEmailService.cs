using System;
using System.Threading.Tasks;

namespace ClashZone.App.Services.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendTournamentRegistrationConfirmationAsync(string toEmail, string tournamentName);
        Task SendMatchReminderAsync(string toEmail, string tournamentName, string opponentName, DateTime? scheduledAt);
        Task SendTournamentReminderAsync(string toEmail, string tournamentName, DateTime startDate);
    }
}