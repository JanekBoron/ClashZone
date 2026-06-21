using ClashZone.App.Services.Interfaces;
using DataAccess;
using Microsoft.EntityFrameworkCore;

namespace ClashZone.Services
{
    public class TournamentReminderBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TournamentReminderBackgroundService> _logger;
        private readonly TimeSpan _pollInterval;
        private readonly TimeSpan _leadTime;

        public TournamentReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<TournamentReminderBackgroundService> logger,
            IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;

            var pollMinutes = config.GetValue<int?>("TournamentReminders:PollIntervalMinutes") ?? 30;
            var leadHours = config.GetValue<int?>("TournamentReminders:LeadTimeHours") ?? 24;
            _pollInterval = TimeSpan.FromMinutes(pollMinutes > 0 ? pollMinutes : 30);
            _leadTime = TimeSpan.FromHours(leadHours > 0 ? leadHours : 24);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "TournamentReminderBackgroundService uruchomiony (poll: {Poll}, okno: {Lead}).",
                _pollInterval, _leadTime);

            using var timer = new PeriodicTimer(_pollInterval);
            do
            {
                try
                {
                    await ProcessRemindersAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd podczas przetwarzania przypomnień o turniejach.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task ProcessRemindersAsync(CancellationToken ct)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.UtcNow;
            var windowEnd = now.Add(_leadTime);

            var dueTournaments = await db.Tournaments
                .Where(t => t.ReminderSentAt == null
                            && t.StartDate > now
                            && t.StartDate <= windowEnd)
                .ToListAsync(ct);

            foreach (var tournament in dueTournaments)
            {
                var emails = await (
                    from tm in db.TeamMembers
                    join team in db.Teams on tm.TeamId equals team.Id
                    join user in db.Users on tm.UserId equals user.Id
                    where team.TournamentId == tournament.Id
                          && user.Email != null
                          && user.Email != ""
                    select user.Email!)
                    .Distinct()
                    .ToListAsync(ct);

                var sent = 0;
                foreach (var email in emails)
                {
                    try
                    {
                        await emailService.SendTournamentReminderAsync(
                            email, tournament.Name, tournament.StartDate);
                        sent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex,
                            "Nie udało się wysłać przypomnienia o turnieju {TournamentId} na adres {Email}.",
                            tournament.Id, email);
                    }
                }

                tournament.ReminderSentAt = now;
                await db.SaveChangesAsync(ct);

                _logger.LogInformation(
                    "Wysłano przypomnienia o turnieju '{Name}' (Id {Id}) do {Sent}/{Total} odbiorców.",
                    tournament.Name, tournament.Id, sent, emails.Count);
            }
        }
    }
}
