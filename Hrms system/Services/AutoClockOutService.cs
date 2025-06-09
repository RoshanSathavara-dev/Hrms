//using Microsoft.EntityFrameworkCore;
//using Hrms_system.Data;
//using Hrms_system.Models;
//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;

//namespace Hrms_system.Services
//{
//    public class AutoClockOutService : BackgroundService
//    {
//        private readonly IServiceProvider _serviceProvider;
//        private readonly ILogger<AutoClockOutService> _logger;
//        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour
//        private readonly TimeSpan _autoClockOutAfter = TimeSpan.FromHours(16); // Auto clock-out after 16 hours

//        public AutoClockOutService(
//            IServiceProvider serviceProvider,
//            ILogger<AutoClockOutService> logger)
//        {
//            _serviceProvider = serviceProvider;
//            _logger = logger;
//        }

//        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//        {
//            while (!stoppingToken.IsCancellationRequested)
//            {
//                try
//                {
//                    await ProcessAutoClockOuts();
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex, "Error occurred while processing auto clock-outs");
//                }

//                await Task.Delay(_checkInterval, stoppingToken);
//            }
//        }

//        private async Task ProcessAutoClockOuts()
//        {
//            using var scope = _serviceProvider.CreateScope();
//            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

//            // Get current time in IST
//            TimeZoneInfo indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
//            DateTime istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);

//            _logger.LogInformation("Checking for auto clock-outs at {CurrentTime}", istNow);

//            // Find all attendance records that:
//            // 1. Have been clocked in
//            // 2. Haven't been clocked out
//            // 3. Have been active for more than 16 hours
//            var recordsToClockOut = await dbContext.Attendance
//                .Where(a => !a.ClockOut.HasValue &&
//                           a.ClockIn.Add(_autoClockOutAfter) <= istNow)
//                .ToListAsync();

//            _logger.LogInformation("Found {Count} records that need auto clock-out", recordsToClockOut.Count);

//            foreach (var record in recordsToClockOut)
//            {
//                try
//                {
//                    record.ClockOut = istNow;
//                    record.IsAutoClockOut = true;
//                    record.AutoClockOutScheduled = false;

//                    _logger.LogInformation(
//                     "Auto clock-out performed for user {UserId} at {ClockOutTime}. Clock-in was at {ClockInTime}",
//                     record.UserId,
//                     istNow,
//                     record.ClockIn);
//                }
//                catch (Exception ex)
//                {
//                    _logger.LogError(ex,
//                        "Error auto clocking out user {UserId}",
//                        record.UserId);
//                }
//            }

//            if (recordsToClockOut.Any())
//            {
//                await dbContext.SaveChangesAsync();
//                _logger.LogInformation("Successfully saved {Count} auto clock-out records", recordsToClockOut.Count);
//            }
//        }
//    }
//}