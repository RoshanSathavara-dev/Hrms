
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hrms_system.Services
{
    public class LeaveAccrualBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<LeaveAccrualBackgroundService> _logger;

        public LeaveAccrualBackgroundService(IServiceProvider serviceProvider, ILogger<LeaveAccrualBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get the next run time (1st of next month at 00:00)
                    var now = DateTime.UtcNow;
                    var nextRun = new DateTime(now.Year, now.Month, 1).AddMonths(1);
                    var delay = nextRun - now;

                    _logger.LogInformation("Next leave accrual run scheduled for {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);

                    // Run accrual
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var leaveAccrualService = scope.ServiceProvider.GetRequiredService<LeaveAccrualService>();
                        await leaveAccrualService.UpdateLeaveAccruals();
                        _logger.LogInformation("Leave accrual completed for {Date}", DateTime.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running leave accrual");
                }
            }
        }
    }
}