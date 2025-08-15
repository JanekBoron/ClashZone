using System;
using System.Threading.Tasks;

namespace ClashZone.Services.Interfaces
{
    /// <summary>
    /// Defines a contract for sending emails within the application.  Additional helper
    /// methods are provided for common notification scenarios such as tournament
    /// registration, match reminders and advancement notifications.
    /// </summary>
    public interface IEmailService
    {
        /// <summary>
        /// Sends a generic HTML email.
        /// </summary>
        /// <param name="toEmail">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="htmlMessage">HTML formatted body.</param>
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);

        /// <summary>
        /// Sends a confirmation that a user has joined a tournament.
        /// </summary>
        /// <param name="toEmail">Recipient address.</param>
        /// <param name="tournamentName">Name of the tournament.</param>
        Task SendTournamentRegistrationConfirmationAsync(string toEmail, string tournamentName);

        /// <summary>
        /// Sends a reminder about an upcoming match to a participant.
        /// </summary>
        /// <param name="toEmail">Recipient address.</param>
        /// <param name="tournamentName">Tournament name.</param>
        /// <param name="opponentName">Opponent's team or user name.</param>
        /// <param name="scheduledAt">Optional date/time when the match is scheduled.</param>
        Task SendMatchReminderAsync(string toEmail, string tournamentName, string opponentName, DateTime? scheduledAt);

        /// <summary>
        /// Sends a notification informing a participant that they have advanced to the next round.
        /// </summary>
        /// <param name="toEmail">Recipient address.</param>
        /// <param name="tournamentName">Tournament name.</param>
        /// <param name="round">The round number the participant has advanced to.</param>
        Task SendPromotionNotificationAsync(string toEmail, string tournamentName, int round);
    }
}