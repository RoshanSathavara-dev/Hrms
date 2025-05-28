using Hrms_system.Data;
using Hrms_system.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Hrms_system.Services
{
    public class LeaveAccrualService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LeaveAccrualService> _logger;

        public LeaveAccrualService(ApplicationDbContext context, ILogger<LeaveAccrualService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task UpdateLeaveAccruals(DateTime? testDate = null)
        {
            var currentDate = testDate ?? DateTime.UtcNow;
            var leaveTypes = await _context.LeaveTypes
                .Where(lt => lt.IsCreditableOnAccrualBasis)
                .ToListAsync();

            foreach (var leaveType in leaveTypes)
            {
                var employees = await _context.Employees
                    .Where(e => e.CompanyId == leaveType.CompanyId)
                    .ToListAsync();

                foreach (var emp in employees)
                {
                    // Get or create balance for the current year
                    var balance = await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == leaveType.Id && b.Year == currentDate.Year);

                    if (balance == null)
                    {
                        // For new balance, set initial leaves based on frequency
                        decimal initialLeaves = leaveType.AccrualFrequency switch
                        {
                            "Monthly" => 1.0m,
                            "Quarterly" => Math.Round(leaveType.LeavesAllowedPerYear / 4, 0),
                            "Yearly" => leaveType.LeavesAllowedPerYear,
                            _ => 0
                        };

                        balance = new EmployeeLeaveBalance
                        {
                            EmployeeId = emp.Id,
                            LeaveTypeId = leaveType.Id,
                            TotalLeaves = initialLeaves,
                            UsedLeaves = 0,
                            PendingLeaves = 0,
                            Year = currentDate.Year,
                            CarryForwardedLeaves = 0,
                            LastAccrualDate = currentDate
                        };
                        _context.EmployeeLeaveBalances.Add(balance);
                        _logger.LogInformation("Created new balance with {InitialLeaves} leaves for Employee {EmployeeId}, LeaveType {LeaveTypeId}, Year {Year}",
                            initialLeaves, emp.Id, leaveType.Id, currentDate.Year);
                        continue; // Skip to next employee since we just created the balance
                    }

                    // Check if accrual is due based on frequency and last accrual date
                    bool shouldAccrue = false;
                    if (balance.LastAccrualDate == null)
                    {
                        // First accrual
                        shouldAccrue = true;
                    }
                    else
                    {
                        switch (leaveType.AccrualFrequency)
                        {
                            case "Monthly":
                                if (leaveType.CreditOnFirstDayOfMonth)
                                {
                                    // Credit on 1st of each month
                                    shouldAccrue = currentDate.Day == 1 &&
                                        currentDate.Date > balance.LastAccrualDate.Value.Date;
                                }
                                else
                                {
                                    // Credit on same date each month
                                    shouldAccrue = currentDate.Date >= balance.LastAccrualDate.Value.AddMonths(1);
                                }
                                break;
                            case "Quarterly":
                                if (leaveType.CreditOnFirstDayOfMonth)
                                {
                                    // Credit on 1st of each quarter (Jan, Apr, Jul, Oct)
                                    shouldAccrue = currentDate.Day == 1 &&
                                        currentDate.Month % 3 == 1 &&
                                        currentDate.Date > balance.LastAccrualDate.Value.Date;
                                }
                                else
                                {
                                    // Credit on same date each quarter
                                    shouldAccrue = currentDate.Date >= balance.LastAccrualDate.Value.AddMonths(3);
                                }
                                break;
                            case "Yearly":
                                if (leaveType.CreditOnFirstDayOfMonth)
                                {
                                    // Credit on 1st of January
                                    shouldAccrue = currentDate.Day == 1 &&
                                        currentDate.Month == 1 &&
                                        currentDate.Date > balance.LastAccrualDate.Value.Date;
                                }
                                else
                                {
                                    // Credit on same date each year
                                    shouldAccrue = currentDate.Date >= balance.LastAccrualDate.Value.AddYears(1);
                                }
                                break;
                        }
                    }

                    if (shouldAccrue)
                    {
                        decimal leavesToAccrue = leaveType.AccrualFrequency switch
                        {
                            "Monthly" => 1.0m,
                            "Quarterly" => Math.Round(leaveType.LeavesAllowedPerYear / 4, 0),
                            "Yearly" => leaveType.LeavesAllowedPerYear,
                            _ => 0
                        };

                        balance.TotalLeaves = Math.Round(balance.TotalLeaves + leavesToAccrue, 0);
                        balance.LastAccrualDate = currentDate;
                        _logger.LogInformation("Accrued {Leaves} leaves for Employee {EmployeeId}, LeaveType {LeaveTypeId}, Year {Year}, NewTotalLeaves={NewTotalLeaves}",
                            leavesToAccrue, emp.Id, leaveType.Id, balance.Year, balance.TotalLeaves);
                    }

                    // Handle carry forward on January 1st
                    if (leaveType.CarryForwardEnabled && leaveType.CarryForwardLimit.HasValue &&
                        currentDate.Month == 1 && currentDate.Day == 1)
                    {
                        var previousYearBalance = await _context.EmployeeLeaveBalances
                            .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == leaveType.Id && b.Year == currentDate.Year - 1);

                        if (previousYearBalance != null)
                        {
                            decimal availableLeaves = previousYearBalance.TotalLeaves - previousYearBalance.UsedLeaves - previousYearBalance.PendingLeaves;
                            decimal carryForward = Math.Min(availableLeaves, leaveType.CarryForwardLimit.Value);
                            balance.CarryForwardedLeaves = Math.Round(carryForward, 0);
                            _logger.LogInformation("Carried forward {CarryForward} leaves for Employee {EmployeeId}, LeaveType {LeaveTypeId}, Year {Year}",
                                carryForward, emp.Id, leaveType.Id, balance.Year);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Leave accruals updated for date {Date}", currentDate);
        }
    }
}