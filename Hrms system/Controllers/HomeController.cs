using System.Diagnostics;
using Hrms_system.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Hrms_system.Data;

namespace Hrms_system.Controllers
{
    [Authorize(Roles = "Employee")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ApplicationDbContext context)
        {
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogWarning("No authenticated user found. Redirecting to login.");
                return RedirectToAction("Login", "Account");
            }

            // Check roles and redirect accordingly
            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            else if (await _userManager.IsInRoleAsync(user, "Employee"))
            {
                // Get the employee record for the current user
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == user.Id);

                if (employee == null)
                {
                    _logger.LogWarning($"No employee record found for user {user.Id}");
                    return RedirectToAction("Login", "Account");
                }

                // Get current month's work hours
                var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                var monthlyAttendance = await _context.Attendance
                    .Where(a => a.EmployeeId == employee.Id &&
                           a.ClockIn >= startOfMonth &&
                           a.ClockIn <= endOfMonth)
                    .ToListAsync();

                var totalWorkHours = monthlyAttendance
                    .Where(a => a.ClockOut != null)
                    .Sum(a => (a.ClockOut!.Value - a.ClockIn).TotalHours);

                // Get leave balance
                var leaveBalances = await _context.EmployeeLeaveBalances
                           .Include(b => b.LeaveType)
                           .Where(b => b.EmployeeId == employee.Id)
                           .Join(_context.LeaveAssignments,
                               balance => new { balance.EmployeeId, balance.LeaveTypeId },
                               assignment => new { assignment.EmployeeId, assignment.LeaveTypeId },
                               (balance, assignment) => new { balance, assignment })
                           .Where(x => x.assignment.IsActive)
                           .Select(x => x.balance)
                           .ToListAsync();

                var totalLeaves = leaveBalances.Sum(b => b.TotalLeaves);
                var availableLeaveBalance = leaveBalances
                    .Where(b => b.AvailableLeaves > 0)
                    .Sum(b => b.AvailableLeaves);

                // Get pending leave requests
                var pendingRequests = await _context.LeaveRequests
                    .CountAsync(l => l.EmployeeId == employee.Id && l.Status == "Pending");

                var recentActivities = await _context.LeaveRequests
           .Include(l => l.LeaveType)
           .Where(l => l.EmployeeId == employee.Id)
           .OrderByDescending(l => l.CreatedAt)
           .Take(4)
           .Select(l => new {
               Date = l.CreatedAt,
               Activity = l.LeaveType != null ? $"Leave Request: {l.LeaveType.Name}" : "Leave Request: Unknown Leave Type",
               Type = "Leave",
               Status = l.Status,
               StartDate = l.StartDate,
               EndDate = l.EndDate
           })
           .ToListAsync();


                // Get today's attendance
                var today = DateTime.Today;
                var userAttendance = await _context.Attendance
                    .FirstOrDefaultAsync(a => a.EmployeeId == employee.Id &&
                                            a.ClockIn.Date == today);

                ViewBag.UserAttendance = userAttendance;
                ViewBag.MonthlyWorkHours = Math.Round(totalWorkHours, 1);
                ViewBag.LeaveBalance = availableLeaveBalance;
                ViewBag.PendingRequests = pendingRequests;
                ViewBag.RecentActivities = recentActivities;

                return View();

            }

            // Fallback for authenticated users without recognized roles
            _logger.LogWarning($"User {user.Email} has no recognized roles. Available roles: {string.Join(",", await _userManager.GetRolesAsync(user))}");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
