using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hrms_system.Models;
using Hrms_system.Data;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Hrms_system.Controllers
{
    [Authorize]
    public class SalaryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalaryController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Salary/Slip
        public async Task<IActionResult> Slip(string? period = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // If no period specified, use current month
            var selectedPeriod = string.IsNullOrEmpty(period)
                ? DateTime.Now
                : DateTime.Parse(period);

            var salarySlip = await _context.SalarySlips
                .Include(s => s.History)
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.EmployeeId == userId &&
                                         s.Period.Month == selectedPeriod.Month &&
                                         s.Period.Year == selectedPeriod.Year);

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(); // or RedirectToAction("Login", "Account");
            }

            salarySlip = await GenerateSampleSlip(userId, selectedPeriod);


            return View(salarySlip);
        }

        // GET: Salary/History
        public async Task<IActionResult> History()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var history = await _context.SalarySlips
                .Where(s => s.EmployeeId == userId)
                .OrderByDescending(s => s.Period)
                .ToListAsync();

            return View(history);
        }

        // GET: Salary/DownloadPdf
        public async Task<IActionResult> DownloadPdf(int id)
        {
            var salarySlip = await _context.SalarySlips
                .Include(s => s.Employee)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (salarySlip == null)
            {
                return NotFound();
            }

            // In a real application, generate PDF here using a library like iTextSharp
            // For now, we'll return a JSON response
            return Json(new
            {
                message = $"PDF for {salarySlip.Period:MMMM yyyy} would be generated here",
                fileName = $"SalarySlip_{salarySlip.Period:yyyyMM}.pdf"
            });
        }

        private async Task<SalarySlip> GenerateSampleSlip(string userId, DateTime period)
        {
            var user = await _context.Users.FindAsync(userId);

            // Create a sample salary slip
            var slip = new SalarySlip
            {
                EmployeeId = userId,
                EmployeeName = user?.FullName ?? "Employee",
                Department = user?.Department ?? "Department",
                Designation = user?.Position ?? "Position",
                JoiningDate = user?.JoiningDate ?? DateTime.Now.AddYears(-1),
                BankAccount = "**** **** **** 4567",
                Period = period,
                IssueDate = DateTime.Now,
                BasicSalary = 4500.00m,
                HouseRentAllowance = 900.00m,
                TransportAllowance = 300.00m,
                MealAllowance = 150.00m,
                PerformanceBonus = 400.00m,
                TaxDeduction = 850.00m,
                SocialSecurity = 150.50m,
                HealthInsurance = 75.00m,
                ProvidentFund = 49.00m,
                WorkingDays = 22,
                PresentDays = 20,
                PaidLeaveTaken = 2,
                SickLeaveTaken = 1,
                LeaveBalance = 12,
                PaymentStatus = "Paid",
                History = new List<SalarySlipHistory>
                {
                    new SalarySlipHistory
                    {
                        Period = period.AddMonths(-1),
                        BasicSalary = 4500.00m,
                        TotalEarnings = 5250.00m,
                        TotalDeductions = 1124.50m,
                        NetPay = 4125.50m,
                        Status = "Paid"
                    },
                    new SalarySlipHistory
                    {
                        Period = period.AddMonths(-2),
                        BasicSalary = 4500.00m,
                        TotalEarnings = 5250.00m,
                        TotalDeductions = 1124.50m,
                        NetPay = 4125.50m,
                        Status = "Paid"
                    }
                }
            };

            return slip;
        }
    }
}