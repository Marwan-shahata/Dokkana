using Dokkana.BLL.Managers;

namespace Dokkana.API.Services
{
    public class ReservationExpiryHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReservationExpiryHostedService> _logger;

        public ReservationExpiryHostedService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<ReservationExpiryHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Reservations:ExpiryEnabled", true))
            {
                _logger.LogInformation("Reservation expiry worker is disabled.");
                return;
            }

            await SweepAsync(stoppingToken);

            using var timer = new PeriodicTimer(GetInterval());
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await SweepAsync(stoppingToken);
        }

        private async Task SweepAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var orders = scope.ServiceProvider.GetRequiredService<IOrderManager>();
                var expired = await orders.CancelExpiredReservationsAsync(GetMaxReservationAge(), stoppingToken);

                if (expired > 0)
                    _logger.LogInformation("Auto-cancelled {Count} expired reservations.", expired);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-cancel expired reservations.");
            }
        }

        private TimeSpan GetMaxReservationAge()
        {
            var hours = _configuration.GetValue("Reservations:ExpiryHours", 24);
            return TimeSpan.FromHours(Math.Clamp(hours, 1, 24 * 30));
        }

        private TimeSpan GetInterval()
        {
            var minutes = _configuration.GetValue("Reservations:ExpirySweepMinutes", 15);
            return TimeSpan.FromMinutes(Math.Clamp(minutes, 1, 24 * 60));
        }
    }
}
