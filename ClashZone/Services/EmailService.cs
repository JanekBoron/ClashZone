using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ClashZone.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClashZone.Services
{
    /// <summary>
    /// Default implementation of <see cref="IEmailService"/> that uses SMTP to send
    /// HTML formatted messages.  SMTP credentials and server details are read
    /// from configuration (appsettings.json or environment variables) under the
    /// "Smtp" section.  This service exposes helpers for common notification
    /// scenarios used throughout the application.
    /// </summary>
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        /// <inheritdoc />
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                throw new ArgumentException("Recipient email must be provided", nameof(toEmail));
            }
            // Read SMTP configuration.  Fallback values are provided for local development.
            var host = _config["Smtp:Host"] ?? "localhost";
            var portString = _config["Smtp:Port"] ?? "25";
            var username = _config["Smtp:Username"];
            var password = _config["Smtp:Password"];
            var from = _config["Smtp:From"] ?? username;
            var useSslString = _config["Smtp:UseSSL"] ?? "true";
            int port = int.TryParse(portString, out var p) ? p : 25;
            bool useSsl = bool.TryParse(useSslString, out var ssl) ? ssl : true;

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = useSsl
            };
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var mail = new MailMessage
            {
                From = new MailAddress(from ?? throw new InvalidOperationException("SMTP From address must be configured.")),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mail.To.Add(toEmail);
            await client.SendMailAsync(mail);
        }

        /// <inheritdoc />
        public Task SendTournamentRegistrationConfirmationAsync(string toEmail, string tournamentName)
        {
            var subject = $"Potwierdzenie rejestracji do turnieju {tournamentName}";
            var body = $"<p>Dziękujemy za dołączenie do turnieju <strong>{WebUtility.HtmlEncode(tournamentName)}</strong>. Życzymy powodzenia w rozgrywkach!</p>";
            return SendEmailAsync(toEmail, subject, body);
        }

        /// <inheritdoc />
        public Task SendMatchReminderAsync(string toEmail, string tournamentName, string opponentName, DateTime? scheduledAt)
        {
            var dateInfo = scheduledAt.HasValue ? $" Mecz odbędzie się {scheduledAt.Value:dd.MM.yyyy HH:mm}." : string.Empty;
            var subject = $"Przypomnienie o nadchodzącym meczu w turnieju {tournamentName}";
            var body = $"<p>Przypominamy o Twoim nadchodzącym meczu w turnieju <strong>{WebUtility.HtmlEncode(tournamentName)}</strong> z przeciwnikiem <strong>{WebUtility.HtmlEncode(opponentName)}</strong>.{dateInfo}</p>";
            return SendEmailAsync(toEmail, subject, body);
        }

        /// <inheritdoc />
        public Task SendPromotionNotificationAsync(string toEmail, string tournamentName, int round)
        {
            var subject = $"Awans do kolejnej rundy w turnieju {tournamentName}";
            var body = $"<p>Gratulacje! Awansowałeś do rundy {round} w turnieju <strong>{WebUtility.HtmlEncode(tournamentName)}</strong>.</p>";
            return SendEmailAsync(toEmail, subject, body);
        }
    }
}