using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Hrms_system.Models;
using Microsoft.EntityFrameworkCore;
using Hrms_system.Data;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.IdentityModel.Tokens;
using Hrms_system.Services;
using Hrms_system.ViewModels;

namespace Hrms_system.Controllers
    {

        [Authorize]
        public class AdminController : Controller
        {


                private readonly UserManager<ApplicationUser> _userManager;
                 private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AdminController> _logger;
        private readonly LeaveAccrualService _leaveAccrualService;

        public AdminController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, SignInManager<ApplicationUser> signInManager , ILogger<AdminController> logger, LeaveAccrualService leaveAccrualService)
                {
                    _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
            _leaveAccrualService = leaveAccrualService;
        }

        public async Task<IActionResult> Index()
        {
            if (!await ValidateAdminStillValid())
            {
                _logger.LogWarning("Admin validation failed, signing out.");
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null || !user.CompanyId.HasValue)
            {
                _logger.LogWarning("User or CompanyId is null, signing out.");
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            var companyId = user.CompanyId.Value;
            var totalEmployees = await _context.Employees
                   .CountAsync(e => e.CompanyId == companyId);

            // Get employees on leave today
            var today = DateTime.Today;
            var onLeaveToday = await _context.LeaveRequests
                .CountAsync(l => l.CompanyId == companyId &&
                                l.Status == "Approved" &&
                                l.StartDate <= today &&
                                l.EndDate >= today);

            // Get pending leave requests
            var pendingRequests = await _context.LeaveRequests
                .CountAsync(l => l.CompanyId == companyId && l.Status == "Pending");

            // Get employees on break now
            var onBreakNow = await _context.Attendance
                .CountAsync(a => a.CompanyId == companyId &&
                                a.BreakStart != null &&
                                a.BreakEnd == null &&
                                a.ClockOut == null);

            var pendingLeaves = await _context.LeaveRequests
                .Where(l => l.Status == "Pending" && l.CompanyId == companyId)
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            var newEmployees = await _context.Employees
                .Where(e => e.CompanyId == companyId && e.JoinDate >= DateTime.Now.AddDays(-30))
                .OrderByDescending(e => e.JoinDate)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.OnLeaveToday = onLeaveToday;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.OnBreakNow = onBreakNow;

            var model = new AdminDashboardViewModel
            {
                PendingLeaveRequests = pendingLeaves,
                NewEmployees = newEmployees
            };

            return View(model);
        }


        private async Task<bool> ValidateAdminStillValid()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User is null in ValidateAdminStillValid.");
                return false;
            }

            var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
            if (!isAdmin)
            {
                _logger.LogWarning("User {UserId} is not in Admin role.", user.Id);
            }

            return isAdmin;
        }


        public async Task<IActionResult> ManageEmployee()
                {
                    var employees = await _userManager.Users.ToListAsync(); // await now used
                    return View(employees);
                }


        public async Task<IActionResult> Attendance(string viewType = "daily", string? month = null, DateTime? fromDate = null, DateTime? toDate = null, string? status = null, string? department = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogWarning("User is null in Attendance action, returning Unauthorized.");
                return Unauthorized();
            }

            int companyId = user.CompanyId ?? 0;
            if (companyId == 0)
            {
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
                if (employee != null)
                {
                    companyId = employee.CompanyId;
                }
                else
                {
                    return BadRequest(new { message = "No employee record or company information found for this user." });
                }
            }

            var today = DateTime.Today;
            // Parse month as yyyy-MM string, default to current month
            DateTime selectedMonth;
            if (!string.IsNullOrEmpty(month) && DateTime.TryParse(month + "-01", out var parsedMonth))
            {
                selectedMonth = parsedMonth;
            }
            else
            {
                selectedMonth = today;
            }

            // Set date range based on viewType
            if (viewType == "monthly")
            {
                // For monthly view, prioritize selectedMonth for the date range
                fromDate = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
                toDate = new DateTime(selectedMonth.Year, selectedMonth.Month, DateTime.DaysInMonth(selectedMonth.Year, selectedMonth.Month));
                // Cap toDate at today to exclude future dates
                if (toDate > today)
                {
                    toDate = today;
                }
            }
            else
            {
                // For daily view, use fromDate and toDate or default to today
                fromDate ??= today;
                toDate ??= today;
            }

            // Ensure dates are valid
            if (fromDate > toDate)
            {
                (fromDate, toDate) = (toDate, fromDate);
            }

            var query = _context.Attendance
                .Include(a => a.Employee)
                  .ThenInclude(e => e.WorkWeekRule)
                .Where(a => a.ClockIn.Date >= fromDate.Value.Date && a.ClockIn.Date <= toDate.Value.Date && a.CompanyId == companyId);

            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Present") query = query.Where(a => a.ClockOut != null);
                else if (status == "Absent") query = query.Where(a => a.ClockOut == null);
                else if (status == "Late") query = query.Where(a => a.ClockIn.TimeOfDay > TimeSpan.FromHours(9));
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(a => a.Employee.Department == department);
            }

            var attendanceList = await query.OrderByDescending(a => a.ClockIn).ToListAsync();
            var employees = await _context.Employees.Where(e => e.CompanyId == companyId).OrderBy(e => e.FirstName).ToListAsync();
            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId) ?? GetDefaultPolicy();

            var holidays = await _context.Holidays
                .Where(h => h.CompanyId == companyId)
                .ToListAsync();

            ViewBag.ViewType = viewType;
            ViewBag.SelectedMonth = selectedMonth.ToString("yyyy-MM");
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.Employees = employees;
            ViewBag.ExpectedWorkHours = policy.ExpectedWorkHours;
            ViewBag.Holidays = holidays;

            return View(attendanceList);
        }


        //public async Task<IActionResult> AssignLeave()
        //{
        //    var employees = await _context.Employees.ToListAsync();
        //    var leaveTypes = await _context.LeaveTypes.ToListAsync();
        //    var viewModel = new List<AssignLeavesViewModel>();

        //    foreach (var employee in employees)
        //    {
        //        var balances = await _context.EmployeeLeaveBalances
        //            .Where(b => b.EmployeeId == employee.Id)
        //            .ToListAsync();

        //        var employeeViewModel = new AssignLeavesViewModel
        //        {
        //            EmployeeId = employee.Id,
        //            EmployeeName = employee.FirstName + " " + employee.LastName,
        //            LeaveTypes = leaveTypes,
        //            CurrentBalance = balances.Sum(b => b.TotalLeaves),
        //            DefaultLeavesAllowed = 0 // Default value, update as needed
        //        };

        //        viewModel.Add(employeeViewModel);
        //    }

        //    return View(viewModel);
        //}
        //[HttpPost]
        //public async Task<IActionResult> AssignLeave(List<AssignLeavesViewModel> model)
        //{
        //    if (model == null || !model.Any())
        //    {
        //        return BadRequest("No data submitted.");
        //    }

        //    foreach (var employee in model)
        //    {
        //        var existingBalances = await _context.EmployeeLeaveBalances
        //            .Where(b => b.EmployeeId == employee.EmployeeId)
        //            .ToListAsync();

        //        foreach (var leaveType in employee.LeaveTypes)
        //        {
        //            var existingBalance = existingBalances.FirstOrDefault(b => b.LeaveTypeId == leaveType.Id);

        //            if (existingBalance == null)
        //            {
        //                var newBalance = new EmployeeLeaveBalance
        //                {
        //                    EmployeeId = employee.EmployeeId,
        //                    LeaveTypeId = leaveType.Id,
        //                    TotalLeaves = employee.DefaultLeavesAllowed,
        //                    UsedLeaves = 0,
        //                    PendingLeaves = 0
        //                };
        //                _context.EmployeeLeaveBalances.Add(newBalance);
        //            }
        //            else
        //            {
        //                existingBalance.TotalLeaves = employee.DefaultLeavesAllowed;
        //                _context.Update(existingBalance);
        //            }
        //        }

        //        // Remove leave types that are not in the submitted list
        //        var leaveTypeIds = employee.LeaveTypes.Select(lt => lt.Id).ToList();
        //        var balancesToRemove = existingBalances.Where(b => !leaveTypeIds.Contains(b.LeaveTypeId)).ToList();
        //        _context.EmployeeLeaveBalances.RemoveRange(balancesToRemove);
        //    }

        //    await _context.SaveChangesAsync();
        //    return RedirectToAction(nameof(AssignLeave));
        //}



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account"); // Redirect to your login page
        }


        public async Task<IActionResult> Leave(string status = "All", int? year = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            int companyId = 0;

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee != null)
                companyId = employee.CompanyId;
            else if (user.CompanyId.HasValue)
                companyId = user.CompanyId.Value;
            else
                return BadRequest("Company not found.");

            // Leave Requests
            var query = _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.LeaveType)
                .Where(l => l.CompanyId == companyId);

            if (status != "All")
                query = query.Where(l => l.Status == status);

            if (year.HasValue)
                query = query.Where(l => l.StartDate.Year == year.Value || l.EndDate.Year == year.Value);

            var leaveRequests = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

            var allRequests = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.Employee != null && l.Employee.CompanyId == companyId)
                .ToListAsync();

            ViewBag.PendingCount = allRequests.Count(l => l.Status == "Pending");
            ViewBag.ApprovedCount = allRequests.Count(l => l.Status == "Approved");
            ViewBag.RejectedCount = allRequests.Count(l => l.Status == "Rejected");
            ViewBag.TodayLeaveCount = allRequests.Count(l =>
                l.Status == "Approved" &&
                l.StartDate <= DateTime.Today &&
                l.EndDate >= DateTime.Today
            );

            ViewBag.CurrentFilter = status;
            ViewBag.SelectedYear = year;
            ViewBag.Years = allRequests.Select(l => l.StartDate.Year)
                                       .Union(allRequests.Select(l => l.EndDate.Year))
                                       .Distinct()
                                       .OrderByDescending(y => y)
                                       .ToList();

            var balances = await _context.EmployeeLeaveBalances
                .Include(b => b.Employee)
                .Include(b => b.LeaveType)
                .Where(b => b.Employee.CompanyId == companyId)
                .ToListAsync();

            // Get all active leave assignments
            var activeAssignments = await _context.LeaveAssignments
                .Where(la => la.IsActive)
                .Select(la => new { la.EmployeeId, la.LeaveTypeId })
                .ToListAsync();

            // Filter balances to only show assigned leaves
            var filteredBalances = balances.Where(b =>
                activeAssignments.Any(a =>
                    a.EmployeeId == b.EmployeeId &&
                    a.LeaveTypeId == b.LeaveTypeId
                )
            ).ToList();

            var viewModel = new LeaveDashboardViewModel
            {
                LeaveRequests = leaveRequests,
                LeaveBalances = filteredBalances
            };


            return View(viewModel);
        }



        public IActionResult Payroll()
        {
            int? companyId = HttpContext.Session.GetInt32("CompanyId");

            var payrollData = _context.Employees
                 .Where(e => e.CompanyId == companyId)
                .Select(e => new PayrollViewModel
                {
                    EmployeeId = e.Id,
                    EmployeeNumber = e.EmployeeNumber,
                    FullName = e.FirstName + " " + e.LastName,
                    Department = e.Department,
                    Salary = e.Salary,
                    Allowances = e.Allowances ?? 0,
                    Deductions = 0,
                    Status = e.Status
                }).ToList();

            return View(payrollData);
        }

        [HttpPost]
        public IActionResult UpdateSalary(int EmployeeId, decimal BasicSalary)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.Id == EmployeeId);
            if (employee != null)
            {
                employee.Salary = BasicSalary;
                _context.SaveChanges();
                return Json(new { success = true, message = "Salary updated." });
            }

            return Json(new { success = false, message = "Employee not found." });
        }


        public IActionResult SalarySlip(int id)
        {
            int? companyId = HttpContext.Session.GetInt32("CompanyId");

            var emp = _context.Employees
                .Where(e => e.Id == id && e.CompanyId == companyId)
                .Select(e => new PayrollViewModel
                {
                    EmployeeId = e.Id,
                    EmployeeNumber = e.EmployeeNumber,
                    FullName = e.FirstName + " " + e.LastName,
                    Department = e.Department,
                    Salary = e.Salary,
                    Allowances = e.Allowances ?? 0,
                    Deductions = 0,
                    //NetPay = (e.Salary + (e.Allowances ?? 0)),
                    Status = e.Status
                }).FirstOrDefault();

            if (emp == null)
            {
                return NotFound();
            }

            return View(emp);
        }



        public IActionResult Reports()
            {
                return View();
            }
        
            public IActionResult Settings()
            {
                return View();
            }


            public IActionResult HolidayCalendar()
            {
            var currentUserCompanyId = GetCurrentUserCompanyId();
            ViewBag.CompanyId = currentUserCompanyId; // Pass to view if needed
            return View();
        }
        [HttpGet]
        public IActionResult GetHolidays()
        {
            var currentUserCompanyId = GetCurrentUserCompanyId();

            var holidays = _context.Holidays
                .Where(h => h.CompanyId == currentUserCompanyId) // Filter by company
                .AsEnumerable()
                .SelectMany(h => new object[]
                {
            new {
                id = h.Id,
                title = "", // Hide title in background event
                start = h.Date.ToString("yyyy-MM-dd"),
                display = "background",
                backgroundColor = "#ffcccc",
                companyId = h.CompanyId // Include company ID in response
            },
            new {
                id = h.Id,
                title = h.Title, // Only this one shows the title
                start = h.Date.ToString("yyyy-MM-dd"),
                color = "#ff4d4d",
                companyId = h.CompanyId // Include company ID in response
            }
                }).ToList();

            return Json(holidays);
        }


        [HttpPost]
        public async Task<IActionResult> AddHoliday([FromBody] Holiday holiday)
        {
            // Set the company ID from current user
            holiday.CompanyId = GetCurrentUserCompanyId();

            _context.Holidays.Add(holiday);
            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpDelete]
        public IActionResult RemoveHoliday([FromQuery] int id)
        {
            if (id == 0)
            {
                return BadRequest("Invalid holiday ID.");
            }

            var currentUserCompanyId = GetCurrentUserCompanyId();
            var holiday = _context.Holidays.FirstOrDefault(h => h.Id == id && h.CompanyId == currentUserCompanyId);

            if (holiday == null)
            {
                return NotFound("Holiday not found or you don't have permission to delete it.");
            }

            _context.Holidays.Remove(holiday);
            _context.SaveChanges();

            return Ok();
        }


        private int GetCurrentUserCompanyId()
        {
            // Implementation depends on your authentication system
            // Example 1: If using ASP.NET Core Identity with CompanyId in claims:
            var companyIdClaim = User.FindFirst("CompanyId");
            if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out int companyId))
            {
                return companyId;
            }

            // Example 2: If you have a User entity with CompanyId:
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = _context.Users.Find(userId);
            return user?.CompanyId ?? 0; // Return 0 or throw if not found
        }

        public async Task<IActionResult> Dashboard()
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            var today = DateTime.Today;
            var model = new AdminAttendanceDashboardViewModel
            {
                TotalEmployees = await _context.Employees.CountAsync(e => e.CompanyId == companyId),
                ClockedInToday = await _context.Attendance
                    .Where(a => a.CompanyId == companyId && a.ClockIn.Date == today && a.ClockOut == null)
                    .CountAsync(),
                LateArrivalsToday = await GetLateArrivalsCount(companyId.Value, today),
                OnBreakNow = await _context.Attendance
            .Where(a => a.CompanyId == companyId &&
                       a.BreakStart != null &&
                       a.BreakEnd == null &&
                       a.ClockOut == null)
            .CountAsync(),
                RecentAttendance = await GetRecentAttendance(companyId.Value)
            };

            return View(model);
        }

        // GET: Attendance policy management
        public async Task<IActionResult> Policy()
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId) ?? new AttendancePolicy
                {
                    CompanyId = companyId.Value,
                    StartTime = new TimeSpan(9, 0, 0), // 9:00 AM default
                    EndTime = new TimeSpan(18, 0, 0),  // 6:00 PM default
                    MaxBreaksPerDay = 2,
                    MaxBreakDurationMinutes = 60,
                    LateGracePeriodMinutes = 15,
                    EarlyDepartureGraceMinutes = 15,
                    ExpectedWorkHours = TimeSpan.FromHours(8),
                };

            return View(policy);
        }

        // POST: Save attendance policy
        [HttpPost]
        public async Task<IActionResult> SavePolicy(AttendancePolicy model)
        {
            if (!ModelState.IsValid)
            {
                return View("Policy", model);
            }

            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            model.CompanyId = companyId.Value;

            // Calculate expected hours based on times for validation
            var calculatedHours = (model.EndTime - model.StartTime) - model.BreakDuration;

            // Option 1: Enforce consistency automatically
            // model.ExpectedWorkHours = calculatedHours;

            // Option 2: Validate consistency (recommended)
            if (model.ExpectedWorkHours != calculatedHours)
            {
                ModelState.AddModelError("ExpectedWorkHours",
                    $"Calculated hours ({calculatedHours:hh\\:mm}) don't match entered value. Adjust times or expected hours.");
                return View("Policy", model);
            }

            var existingPolicy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId);

            if (existingPolicy == null)
            {
                _context.AttendancePolicies.Add(model);
            }
            else
            {
                // Update all fields including ExpectedWorkHours
                existingPolicy.StartTime = model.StartTime;
                existingPolicy.EndTime = model.EndTime;
                existingPolicy.BreakDuration = model.BreakDuration;
                existingPolicy.ExpectedWorkHours = model.ExpectedWorkHours;
                existingPolicy.MaxBreaksPerDay = model.MaxBreaksPerDay;
                existingPolicy.MaxBreakDurationMinutes = model.MaxBreakDurationMinutes;
                existingPolicy.LateGracePeriodMinutes = model.LateGracePeriodMinutes;
                existingPolicy.EarlyDepartureGraceMinutes = model.EarlyDepartureGraceMinutes;

                _context.Update(existingPolicy);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Attendance policy updated successfully";
            return RedirectToAction("Policy");
        }

        public async Task<IActionResult> ComplianceReport(DateTime? fromDate, DateTime? toDate)
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            fromDate ??= DateTime.Today.AddDays(-30);
            toDate ??= DateTime.Today;

            var query = _context.Attendance
                .Include(a => a.Employee)
                .Where(a => a.CompanyId == companyId &&
                           a.ClockIn.Date >= fromDate.Value.Date &&
                           a.ClockIn.Date <= toDate.Value.Date);

            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId) ?? GetDefaultPolicy();

            var records = await query.ToListAsync();
            var report = records.Select(a => new AttendanceComplianceViewModel
            {
                EmployeeId = a.EmployeeId ?? 0,
                EmployeeName = a.Employee != null ? $"{a.Employee.FirstName} {a.Employee.LastName}" : "Unknown",
                Date = a.ClockIn.Date,
                ClockIn = a.ClockIn,
                ClockOut = a.ClockOut,
                BreakDuration = a.TotalBreakDuration,
                ExpectedHours = policy.ExpectedWorkHours,
                ActualHours = CalculateActualHours(a, policy),
                Status = GetComplianceStatus(a, policy)
            }).ToList();

            ViewBag.FromDate = fromDate.Value;
            ViewBag.ToDate = toDate.Value;

            return View(report);
        }

        // GET: Employee attendance details
        public async Task<IActionResult> EmployeeAttendance(int employeeId, DateTime? selectedDate)
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            // Set default to current month if not provided
            selectedDate ??= DateTime.Today;

            // Calculate date range for the selected month
            var fromDate = new DateTime(selectedDate.Value.Year, selectedDate.Value.Month, 1);
            var toDate = fromDate.AddMonths(1).AddDays(-1);

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null || employee.CompanyId != companyId)
            {
                return NotFound();
            }


            var attendanceRecords = await _context.Attendance
    .Include(a => a.BreakLogs)
    .Where(a => a.EmployeeId == employeeId &&
               a.ClockIn.Date >= fromDate &&
               a.ClockIn.Date <= toDate)
    .OrderByDescending(a => a.ClockIn)
    .ToListAsync();

            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId) ?? GetDefaultPolicy();

            var records = await _context.Attendance
                .Where(a => a.EmployeeId == employeeId &&
                           a.ClockIn.Date >= fromDate.Date &&
                           a.ClockIn.Date <= toDate.Date)
                .OrderByDescending(a => a.ClockIn)
                .ToListAsync();

            var model = new EmployeeAttendanceViewModel
            {
                Employee = employee,
                Records = records.Select(a => new EmployeeAttendanceRecordViewModel
                {
                    Id = a.Id,
                    Date = a.ClockIn.Date,
                    ClockIn = a.ClockIn,
                    ClockOut = a.ClockOut,
                    BreakDuration = a.TotalBreakDuration,
                    ExpectedHours = policy.ExpectedWorkHours,
                    ActualHours = CalculateActualHours(a, policy),
                    BreakLogs = a.BreakLogs.ToList(),
                    Status = GetComplianceStatus(a, policy),
                    IsLate = IsLateArrival(a.ClockIn, policy),
                    IsEarlyDeparture = IsEarlyDeparture(a.ClockOut, policy)
                }).ToList(),
                FromDate = fromDate,
                ToDate = toDate,
                Summary = CalculateEmployeeSummary(records, policy),
                SelectedDate = selectedDate.Value // Store the selected date
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendancePolicy()
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId.Value);

            if (policy == null)
            {
                policy = GetDefaultPolicy();
                policy.CompanyId = companyId.Value;
            }

            return Ok(new
            {
                maxBreaksPerDay = policy.MaxBreaksPerDay
            });
        }
        [HttpPost]
        public async Task<IActionResult> AdjustAttendance(int id, DateTime clockIn, DateTime? clockOut)
        {
            var record = await _context.Attendance.FindAsync(id);
            if (record == null)
            {
                return NotFound();
            }

            // Verify the admin has access to this record
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            var employee = await _context.Employees.FindAsync(record.EmployeeId);
            if (companyId == null || employee?.CompanyId != companyId)
            {
                return Unauthorized();
            }

            record.ClockIn = clockIn;
            record.ClockOut = clockOut;
            record.IsManualEntry = true;


            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // Helper methods
        private async Task<int> GetLateArrivalsCount(int companyId, DateTime date)
        {
            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId) ?? GetDefaultPolicy();

            var lateThreshold = policy.StartTime.Add(TimeSpan.FromMinutes(policy.LateGracePeriodMinutes));

            return await _context.Attendance
                .Where(a => a.CompanyId == companyId &&
                           a.ClockIn.Date == date &&
                           a.ClockIn.TimeOfDay > lateThreshold)
                .CountAsync();
        }

        private async Task<List<RecentAttendanceViewModel>> GetRecentAttendance(int companyId)
        {
            return await _context.Attendance
                .Include(a => a.Employee)
                .Where(a => a.CompanyId == companyId)
                .OrderByDescending(a => a.ClockIn)
                .Take(10)
                .Select(a => new RecentAttendanceViewModel
                {
                    EmployeeName = $"{a.Employee.FirstName} {a.Employee.LastName}",
                    ClockIn = a.ClockIn,
                    ClockOut = a.ClockOut,
                    Status = a.ClockOut == null ? "Clocked In" : "Clocked Out"
                })
                .ToListAsync();
        }

        private TimeSpan CalculateActualHours(Attendance attendance, AttendancePolicy policy)
        {
            if (!attendance.ClockOut.HasValue)
            {
                return TimeSpan.Zero;
            }

            var workDuration = attendance.ClockOut.Value - attendance.ClockIn;
            return workDuration - attendance.TotalBreakDuration;
        }

        private string GetComplianceStatus(Attendance attendance, AttendancePolicy policy)
        {
            if (!attendance.ClockOut.HasValue)
            {
                return "Pending";
            }

            var actualHours = CalculateActualHours(attendance, policy);
            var expectedHours = policy.ExpectedWorkHours;

            if (actualHours >= expectedHours)
            {
                return "Complete";
            }

            var deficit = expectedHours - actualHours;
            return $"Short by {deficit:hh\\:mm}";
        }

        private bool IsLateArrival(DateTime clockIn, AttendancePolicy policy)
        {
            var lateThreshold = policy.StartTime.Add(TimeSpan.FromMinutes(policy.LateGracePeriodMinutes));
            return clockIn.TimeOfDay > lateThreshold;
        }

        private bool IsEarlyDeparture(DateTime? clockOut, AttendancePolicy policy)
        {
            if (!clockOut.HasValue) return false;

            var earlyThreshold = policy.EndTime.Subtract(TimeSpan.FromMinutes(policy.EarlyDepartureGraceMinutes));
            return clockOut.Value.TimeOfDay < earlyThreshold;
        }

        private EmployeeAttendanceSummary CalculateEmployeeSummary(List<Attendance> records, AttendancePolicy policy)
        {
            var summary = new EmployeeAttendanceSummary();
            var completedRecords = records.Where(r => r.ClockOut.HasValue).ToList();

            summary.TotalDays = completedRecords.Count;
            summary.TotalHours = TimeSpan.FromTicks(completedRecords.Sum(r => CalculateActualHours(r, policy).Ticks));
            summary.AverageHoursPerDay = summary.TotalDays > 0
                ? TimeSpan.FromTicks(summary.TotalHours.Ticks / summary.TotalDays)
                : TimeSpan.Zero;
            summary.LateArrivals = completedRecords.Count(r => IsLateArrival(r.ClockIn, policy));
            summary.EarlyDepartures = completedRecords.Count(r => IsEarlyDeparture(r.ClockOut, policy));
            summary.MissedDays = 0; // Would need expected work days to calculate this

            return summary;
        }

        private AttendancePolicy GetDefaultPolicy()
        {
            return new AttendancePolicy
            {
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BreakDuration = TimeSpan.FromHours(1),
                LateGracePeriodMinutes = 15,
                EarlyDepartureGraceMinutes = 15,
                MaxBreaksPerDay = 2,
                MaxBreakDurationMinutes = 60
            };
        }

        public async Task<IActionResult> ExportComplianceReport(DateTime fromDate, DateTime toDate)
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null) return Unauthorized();

            var query = _context.Attendance
                .Include(a => a.Employee)
                .Where(a => a.CompanyId == companyId &&
                           a.ClockIn.Date >= fromDate.Date &&
                           a.ClockIn.Date <= toDate.Date);

            var policy = await _context.AttendancePolicies
                .FirstOrDefaultAsync(p => p.CompanyId == companyId) ?? GetDefaultPolicy();

            var records = await query.ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Compliance Report");

                // Headers
                worksheet.Cell(1, 1).Value = "Employee";
                worksheet.Cell(1, 2).Value = "Date";
                worksheet.Cell(1, 3).Value = "Clock In";
                worksheet.Cell(1, 4).Value = "Clock Out";
                worksheet.Cell(1, 5).Value = "Break Duration";
                worksheet.Cell(1, 6).Value = "Expected Hours";
                worksheet.Cell(1, 7).Value = "Actual Hours";
                worksheet.Cell(1, 8).Value = "Status";

                int row = 2;
                foreach (var record in records)
                {
                    var actualHours = CalculateActualHours(record, policy);
                    var status = GetComplianceStatus(record, policy);

                    worksheet.Cell(row, 1).Value = $"{record.Employee?.FirstName} {record.Employee?.LastName}";
                    worksheet.Cell(row, 2).Value = record.ClockIn.Date;
                    worksheet.Cell(row, 3).Value = record.ClockIn;
                    worksheet.Cell(row, 4).Value = record.ClockOut;
                    worksheet.Cell(row, 5).Value = record.TotalBreakDuration.ToString(@"hh\:mm");
                    worksheet.Cell(row, 6).Value = policy.ExpectedWorkHours.ToString(@"hh\:mm");
                    worksheet.Cell(row, 7).Value = actualHours.ToString(@"hh\:mm");
                    worksheet.Cell(row, 8).Value = status;

                    // Highlight non-compliant rows
                    if (status != "Complete")
                    {
                        worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    }

                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        $"ComplianceReport_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}.xlsx");
                }
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetMonthlyAttendanceData()
        {
            var companyId = HttpContext.Session.GetInt32("CompanyId");

            if (companyId == null)
            {
                return StatusCode(500, new { error = "CompanyId not found in session" });
            }

            var currentMonth = DateTime.Now.Month;
            var currentYear = DateTime.Now.Year;

            var attendance = await _context.Attendance
                .Include(a => a.Employee)
                .Where(a => a.Employee != null &&
                            a.Employee.CompanyId == companyId &&
                            a.ClockIn.Month == currentMonth &&
                            a.ClockIn.Year == currentYear)
                .GroupBy(a => a.ClockIn.Day)
                .Select(g => new
                {
                    Day = g.Key,
                    Present = g.Count(a => a.ClockOut.HasValue),  // Present if ClockOut is not null
                    Absent = g.Count(a => !a.ClockOut.HasValue)   // Absent if ClockOut is null
                })
                .OrderBy(g => g.Day)
                .ToListAsync();

            var labels = attendance.Select(a => $"Day {a.Day}").ToList();
            var present = attendance.Select(a => a.Present).ToList();
            var absent = attendance.Select(a => a.Absent).ToList();

            return Json(new { labels, present, absent });
        }


        // GET: Leave Types
        public async Task<IActionResult> LeaveTypes()
        {
            var user = await _userManager.GetUserAsync(User);
            var companyId = user.CompanyId ?? 0;

            var leaveTypes = await _context.LeaveTypes
                .Where(lt => lt.CompanyId == companyId)
                .ToListAsync();

            return View(leaveTypes);
        }

        // GET: Create Leave Type
        public IActionResult CreateLeaveType()
        {
            return View(new LeaveType());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLeaveType(LeaveType model)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state invalid: {Errors}",
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return View(model);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null || user.CompanyId == null)
                {
                    return RedirectToAction("Index", "Home");
                }

                model.CompanyId = user.CompanyId.Value;

                // For Loss of Pay leaves, set LeavesAllowedPerYear to 0
                if (model.IsLossOfPay)
                {
                    model.LeavesAllowedPerYear = 0;
                    model.IsCreditableOnAccrualBasis = false;
                    model.CarryForwardEnabled = false;
                }

                model.CreatedAt = DateTime.UtcNow;
                _context.LeaveTypes.Add(model);
                await _context.SaveChangesAsync();

                var employees = await _context.Employees
                    .Where(e => e.CompanyId == model.CompanyId)
                    .ToListAsync();

                foreach (var emp in employees)
                {
                    var alreadyExists = await _context.EmployeeLeaveBalances
                        .AnyAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == model.Id);

                    if (!alreadyExists)
                    {
                        decimal initialLeaves = 0;
                        if (model.IsCreditableOnAccrualBasis)
                        {
                            if (model.CreditOnFirstDayOfMonth)
                            {
                                // If credit on 1st of month is checked, don't give initial leaves
                                // They will be credited on the 1st of next month
                                initialLeaves = 0;
                            }
                            else
                            {
                                // If credit on same date, give initial leaves
                                initialLeaves = CalculateInitialAccrual(model);
                            }
                        }
                        else
                        {
                            initialLeaves = model.LeavesAllowedPerYear;
                        }

                        _context.EmployeeLeaveBalances.Add(new EmployeeLeaveBalance
                        {
                            EmployeeId = emp.Id,
                            LeaveTypeId = model.Id,
                            TotalLeaves = initialLeaves,
                            UsedLeaves = 0,
                            PendingLeaves = 0,
                            Year = DateTime.UtcNow.Year,
                            CarryForwardedLeaves = 0,
                            LastAccrualDate = model.CreditOnFirstDayOfMonth ? null : DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Initialized leave balances for leave type {LeaveTypeId}", model.Id);
                return RedirectToAction("LeaveTypes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave type {Name}", model.Name);
                ModelState.AddModelError("", "An error occurred while creating the leave type.");
                return View(model);
            }
        }
        //public async Task UpdateLeaveAccruals()
        //{
        //    var leaveTypes = await _context.LeaveTypes
        //        .Where(lt => lt.IsCreditableOnAccrualBasis)
        //        .ToListAsync();

        //    foreach (var leaveType in leaveTypes)
        //    {
        //        var employees = await _context.Employees
        //            .Where(e => e.CompanyId == leaveType.CompanyId)
        //            .ToListAsync();

        //        foreach (var emp in employees)
        //        {
        //            var balance = await _context.EmployeeLeaveBalances
        //                .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id && b.LeaveTypeId == leaveType.Id && b.Year == DateTime.UtcNow.Year);

        //            if (balance == null)
        //            {
        //                balance = new EmployeeLeaveBalance
        //                {
        //                    EmployeeId = emp.Id,
        //                    LeaveTypeId = leaveType.Id,
        //                    TotalLeaves = 0,
        //                    UsedLeaves = 0,
        //                    PendingLeaves = 0,
        //                    Year = DateTime.UtcNow.Year,
        //                    CarryForwardedLeaves = 0
        //                };
        //                _context.EmployeeLeaveBalances.Add(balance);
        //            }

        //            decimal accruedLeaves = CalculateAccrual(leaveType);
        //            balance.TotalLeaves += accruedLeaves;

        //            // Handle carry forward
        //            if (leaveType.CarryForwardEnabled && leaveType.CarryForwardLimit.HasValue)
        //            {
        //                decimal availableLeaves = balance.TotalLeaves - balance.UsedLeaves - balance.PendingLeaves;
        //                decimal carryForward = Math.Min(availableLeaves, leaveType.CarryForwardLimit.Value);

        //                balance.CarryForwardedLeaves = carryForward;
        //                balance.CarryForwardExpiry = leaveType.CarryForwardExpiryInMonths.HasValue
        //                    ? DateTime.UtcNow.AddMonths(leaveType.CarryForwardExpiryInMonths.Value)
        //                    : null;

        //                // Reset non-carried forward leaves for the new year
        //                if (DateTime.UtcNow.Month == 1 && balance.Year < DateTime.UtcNow.Year)
        //                {
        //                    balance.TotalLeaves = carryForward;
        //                    balance.Year = DateTime.UtcNow.Year;
        //                }
        //            }
        //        }
        //    }

        //    await _context.SaveChangesAsync();
        //}

        private decimal CalculateInitialAccrual(LeaveType leaveType)
        {
            if (!leaveType.IsCreditableOnAccrualBasis) return 0;

            return leaveType.AccrualFrequency switch
            {
                "Monthly" => Math.Round(leaveType.LeavesAllowedPerYear / 12, 0), // Calculate monthly leaves from yearly allowance
                "Quarterly" => Math.Round(leaveType.LeavesAllowedPerYear / 4, 0),
                "Yearly" => leaveType.LeavesAllowedPerYear,
                _ => 0
            };
        }
        private decimal CalculateAccrual(LeaveType leaveType)
        {
            // Simulate accrual based on current date
            bool shouldAccrue = leaveType.AccrualFrequency switch
            {
                "Monthly" => DateTime.UtcNow.Day == 1, // Accrue on the 1st of each month
                "Quarterly" => DateTime.UtcNow.Month % 3 == 1 && DateTime.UtcNow.Day == 1, // Accrue on the 1st of Jan, Apr, Jul, Oct
                "Yearly" => DateTime.UtcNow.Month == 1 && DateTime.UtcNow.Day == 1, // Accrue on Jan 1st
                _ => false
            };

            if (!shouldAccrue) return 0;

            return leaveType.AccrualFrequency switch
            {
                "Monthly" => leaveType.LeavesAllowedPerYear / 12,
                "Quarterly" => leaveType.LeavesAllowedPerYear / 4,
                "Yearly" => leaveType.LeavesAllowedPerYear,
                _ => 0
            };
        }
        //[HttpGet]
        //public async Task<IActionResult> TestAccrual(DateTime? testDate = null)
        //{
        //    try
        //    {
        //        // If no date provided, use current date
        //        var date = testDate ?? DateTime.Now;
        //        ViewBag.TestDate = date;

        //        // Get all leave types with accrual basis
        //        var leaveTypes = await _context.LeaveTypes
        //            .Where(lt => lt.IsCreditableOnAccrualBasis)
        //            .ToListAsync();

        //        var results = new List<object>();

        //        foreach (var leaveType in leaveTypes)
        //        {
        //            var employees = await _context.Employees
        //                .Where(e => e.CompanyId == leaveType.CompanyId)
        //                .ToListAsync();

        //            foreach (var emp in employees)
        //            {
        //                var balance = await _context.EmployeeLeaveBalances
        //                    .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id &&
        //                                            b.LeaveTypeId == leaveType.Id &&
        //                                            b.Year == date.Year);

        //                if (balance != null)
        //                {
        //                    // Calculate accrual amount for the test date
        //                    bool wouldAccrue = ShouldAccrueOnDate(leaveType, balance, date);
        //                    decimal accrualAmount = 0;

        //                    if (wouldAccrue)
        //                    {
        //                        accrualAmount = leaveType.AccrualFrequency switch
        //                        {
        //                            "Monthly" => Math.Round(leaveType.LeavesAllowedPerYear / 12, 0),
        //                            "Quarterly" => Math.Round(leaveType.LeavesAllowedPerYear / 4, 0),
        //                            "Yearly" => leaveType.LeavesAllowedPerYear,
        //                            _ => 0
        //                        };
        //                    }

        //                    // Calculate cumulative accrual up to the test date
        //                    decimal cumulativeAccrual = 0;
        //                    DateTime startDate = balance.LastAccrualDate ?? new DateTime(date.Year, 1, 1);

        //                    if (leaveType.CreditOnFirstDayOfMonth)
        //                    {
        //                        // For credit on 1st, count months from start of year or last accrual
        //                        int monthsToAccrue = (date.Year - startDate.Year) * 12 + date.Month - startDate.Month;
        //                        if (date.Day == 1) monthsToAccrue++; // Include current month if it's the 1st

        //                        cumulativeAccrual = monthsToAccrue * Math.Round(leaveType.LeavesAllowedPerYear / 12, 0);
        //                    }
        //                    else
        //                    {
        //                        // For credit on same date, count complete periods
        //                        switch (leaveType.AccrualFrequency)
        //                        {
        //                            case "Monthly":
        //                                int monthsToAccrue = (date.Year - startDate.Year) * 12 + date.Month - startDate.Month;
        //                                if (date.Day >= startDate.Day) monthsToAccrue++;
        //                                cumulativeAccrual = monthsToAccrue * Math.Round(leaveType.LeavesAllowedPerYear / 12, 0);
        //                                break;

        //                            case "Quarterly":
        //                                int quartersToAccrue = ((date.Year - startDate.Year) * 4) + ((date.Month - 1) / 3) - ((startDate.Month - 1) / 3);
        //                                if (date.Day >= startDate.Day) quartersToAccrue++;
        //                                cumulativeAccrual = quartersToAccrue * Math.Round(leaveType.LeavesAllowedPerYear / 4, 0);
        //                                break;

        //                            case "Yearly":
        //                                int yearsToAccrue = date.Year - startDate.Year;
        //                                if (date.Month > startDate.Month || (date.Month == startDate.Month && date.Day >= startDate.Day)) yearsToAccrue++;
        //                                cumulativeAccrual = yearsToAccrue * leaveType.LeavesAllowedPerYear;
        //                                break;
        //                        }
        //                    }

        //                    results.Add(new
        //                    {
        //                        EmployeeName = emp.FullName,
        //                        LeaveType = leaveType.Name,
        //                        CurrentBalance = balance.TotalLeaves,
        //                        AccrualAmount = accrualAmount,
        //                        CumulativeAccrual = cumulativeAccrual,
        //                        ProjectedBalance = balance.TotalLeaves + cumulativeAccrual,
        //                        LastAccrualDate = balance.LastAccrualDate,
        //                        CreditOnFirstDay = leaveType.CreditOnFirstDayOfMonth,
        //                        AccrualFrequency = leaveType.AccrualFrequency,
        //                        TestDate = date,
        //                        WouldAccrue = wouldAccrue
        //                    });
        //                }
        //            }
        //        }

        //        return View(results);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error testing accrual");
        //        return StatusCode(500, "An error occurred during test accrual.");
        //    }
        //}

        [HttpGet]
        public async Task<IActionResult> TestAccrual(DateTime? testDate = null)
        {
            try
            {
                // If no date provided, use current date
                var date = testDate ?? DateTime.Now;
                ViewBag.TestDate = date;

                // Get all leave types with accrual basis
                var leaveTypes = await _context.LeaveTypes
                    .Where(lt => lt.IsCreditableOnAccrualBasis)
                    .ToListAsync();

                var results = new List<object>();

                foreach (var leaveType in leaveTypes)
                {
                    var employees = await _context.Employees
                        .Where(e => e.CompanyId == leaveType.CompanyId)
                        .ToListAsync();

                    foreach (var emp in employees)
                    {
                        var balance = await _context.EmployeeLeaveBalances
                            .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id &&
                                                    b.LeaveTypeId == leaveType.Id &&
                                                    b.Year == date.Year);

                        if (balance != null)
                        {
                            // Calculate accrual amount for the test date
                            bool wouldAccrue = ShouldAccrueOnDate(leaveType, balance, date);
                            decimal accrualAmount = 0;

                            if (wouldAccrue)
                            {
                                accrualAmount = leaveType.AccrualFrequency switch
                                {
                                    "Monthly" => Math.Round(leaveType.LeavesAllowedPerYear / 12, 0),
                                    "Quarterly" => Math.Round(leaveType.LeavesAllowedPerYear / 4, 0),
                                    "Yearly" => leaveType.LeavesAllowedPerYear,
                                    _ => 0
                                };
                            }

                            // Calculate cumulative accrual based on join date
                            decimal cumulativeAccrual = 0;
                            DateTime startDate = emp.JoinDate;
                            DateTime endDate = date;

                            // Handle carry forward from previous year
                            decimal carryForwardAmount = 0;
                            if (date.Year > startDate.Year && leaveType.CarryForwardEnabled)
                            {
                                var previousYearBalance = await _context.EmployeeLeaveBalances
                                    .FirstOrDefaultAsync(b => b.EmployeeId == emp.Id &&
                                                            b.LeaveTypeId == leaveType.Id &&
                                                            b.Year == date.Year - 1);

                                if (previousYearBalance != null)
                                {
                                    decimal availableLeaves = previousYearBalance.TotalLeaves -
                                                           previousYearBalance.UsedLeaves -
                                                           previousYearBalance.PendingLeaves;

                                    carryForwardAmount = Math.Min(availableLeaves,
                                        leaveType.CarryForwardLimit ?? availableLeaves);
                                }
                            }

                            if (leaveType.CreditOnFirstDayOfMonth)
                            {
                                // For credit on 1st, count months from join date
                                int monthsToAccrue = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
                                if (endDate.Day == 1) monthsToAccrue++; // Include current month if it's the 1st

                                // Don't count months before join date
                                if (startDate.Day > 1)
                                {
                                    monthsToAccrue--;
                                }

                                cumulativeAccrual = monthsToAccrue * Math.Round(leaveType.LeavesAllowedPerYear / 12, 0);
                            }
                            else
                            {
                                // For credit on same date, count complete periods
                                switch (leaveType.AccrualFrequency)
                                {
                                    case "Monthly":
                                        int monthsToAccrue = (endDate.Year - startDate.Year) * 12 + endDate.Month - startDate.Month;
                                        if (endDate.Day >= startDate.Day) monthsToAccrue++;
                                        cumulativeAccrual = monthsToAccrue * Math.Round(leaveType.LeavesAllowedPerYear / 12, 0);
                                        break;

                                    case "Quarterly":
                                        int quartersToAccrue = ((endDate.Year - startDate.Year) * 4) +
                                                             ((endDate.Month - 1) / 3) - ((startDate.Month - 1) / 3);
                                        if (endDate.Day >= startDate.Day) quartersToAccrue++;
                                        cumulativeAccrual = quartersToAccrue * Math.Round(leaveType.LeavesAllowedPerYear / 4, 0);
                                        break;

                                    case "Yearly":
                                        int yearsToAccrue = endDate.Year - startDate.Year;
                                        if (endDate.Month > startDate.Month ||
                                            (endDate.Month == startDate.Month && endDate.Day >= startDate.Day))
                                            yearsToAccrue++;
                                        cumulativeAccrual = yearsToAccrue * leaveType.LeavesAllowedPerYear;
                                        break;
                                }
                            }

                            // Add carry forward to cumulative accrual
                            cumulativeAccrual += carryForwardAmount;

                            results.Add(new
                            {
                                EmployeeName = emp.FullName,
                                LeaveType = leaveType.Name,
                                CurrentBalance = balance.TotalLeaves,
                                AccrualAmount = accrualAmount,
                                CumulativeAccrual = cumulativeAccrual,
                                CarryForwardAmount = carryForwardAmount,
                                ProjectedBalance = balance.TotalLeaves + cumulativeAccrual,
                                LastAccrualDate = balance.LastAccrualDate,
                                CreditOnFirstDay = leaveType.CreditOnFirstDayOfMonth,
                                AccrualFrequency = leaveType.AccrualFrequency,
                                JoinDate = emp.JoinDate,
                                TestDate = date,
                                WouldAccrue = wouldAccrue
                            });
                        }
                    }
                }

                return View(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing accrual");
                return StatusCode(500, "An error occurred during test accrual.");
            }
        }




        private bool ShouldAccrueOnDate(LeaveType leaveType, EmployeeLeaveBalance balance, DateTime testDate)
        {
            if (balance.LastAccrualDate == null)
            {
                return !leaveType.CreditOnFirstDayOfMonth || testDate.Day == 1;
            }

            switch (leaveType.AccrualFrequency)
            {
                case "Monthly":
                    if (leaveType.CreditOnFirstDayOfMonth)
                    {
                        return testDate.Day == 1 && testDate.Date > balance.LastAccrualDate.Value.Date;
                    }
                    return testDate.Date >= balance.LastAccrualDate.Value.AddMonths(1);

                case "Quarterly":
                    if (leaveType.CreditOnFirstDayOfMonth)
                    {
                        return testDate.Day == 1 &&
                               testDate.Month % 3 == 1 &&
                               testDate.Date > balance.LastAccrualDate.Value.Date;
                    }
                    return testDate.Date >= balance.LastAccrualDate.Value.AddMonths(3);

                case "Yearly":
                    if (leaveType.CreditOnFirstDayOfMonth)
                    {
                        return testDate.Day == 1 &&
                               testDate.Month == 1 &&
                               testDate.Date > balance.LastAccrualDate.Value.Date;
                    }
                    return testDate.Date >= balance.LastAccrualDate.Value.AddYears(1);

                default:
                    return false;
            }
        }


        //[HttpGet]
        //public async Task<IActionResult> ViewMonthlyLeaveBalances()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null || !user.CompanyId.HasValue)
        //    {
        //        _logger.LogWarning("User or CompanyId is null, signing out.");
        //        await _signInManager.SignOutAsync();
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var companyId = user.CompanyId.Value;

        //    var balances = await _context.EmployeeLeaveBalances
        //        .Include(b => b.Employee)
        //        .Include(b => b.LeaveType)
        //        .Where(b => b.Employee.CompanyId == companyId)
        //        .Select(b => new LeaveBalanceViewModel
        //        {
        //            EmployeeName = b.Employee.FirstName + " " + b.Employee.LastName,
        //            LeaveTypeName = b.LeaveType.Name,
        //            TotalLeaves = b.TotalLeaves,
        //            UsedLeaves = b.UsedLeaves,
        //            PendingLeaves = b.PendingLeaves,
        //            AvailableLeaves = b.TotalLeaves - b.UsedLeaves - b.PendingLeaves + (b.CarryForwardedLeaves ?? 0),
        //            CarryForwardedLeaves = b.CarryForwardedLeaves,
                  
        //            Year = b.Year
        //        })
        //        .ToListAsync();

        //    return View(balances);
        //}

        [HttpGet]
        public async Task<IActionResult> TestCarryForward()
        {
            try
            {
                var testDate = new DateTime(2026, 1, 1);
                await _leaveAccrualService.UpdateLeaveAccruals(testDate);
                _logger.LogInformation("Test carry forward executed for date {TestDate}", testDate);
                return RedirectToAction("ViewMonthlyLeaveBalances"); // Redirect to Leave action
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing test carry forward");
                return StatusCode(500, "An error occurred during test carry forward.");
            }
        }

        // GET: Edit Leave Type
        // GET: Edit Leave Type
        public async Task<IActionResult> EditLeaveType(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null)
            {
                return NotFound();
            }

            return View(leaveType);
        }

        // POST: Edit Leave Type
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLeaveType(int id, LeaveType model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // For Loss of Pay leaves, set LeavesAllowedPerYear to 0
                    if (model.IsLossOfPay)
                    {
                        model.LeavesAllowedPerYear = 0;
                        model.IsCreditableOnAccrualBasis = false;
                        model.CarryForwardEnabled = false;
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LeaveTypeExists(model.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(model);
        }


        // POST: Delete Leave Type
        [HttpPost]
        public async Task<IActionResult> DeleteLeaveType(int id)
        {
            var leaveType = await _context.LeaveTypes.FindAsync(id);
            if (leaveType == null)
            {
                return NotFound();
            }

            _context.LeaveTypes.Remove(leaveType);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(LeaveTypes));
        }

        // GET: Assign Leaves to Employees
        // Controllers/AdminLeaveController.cs
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> AssignLeaves()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    var companyId = user.CompanyId ?? 0;

        //    var employees = await _context.Employees
        //        .Where(e => e.CompanyId == companyId)
        //        .ToListAsync();

        //    var leaveTypes = await _context.LeaveTypes
        //        .Where(lt => lt.CompanyId == companyId)
        //        .ToListAsync();

        //    // Create view models for each employee-leave type combination
        //    var viewModels = new List<AssignLeavesViewModel>();

        //    foreach (var employee in employees)
        //    {
        //        foreach (var leaveType in leaveTypes)
        //        {
        //            var balance = await _context.EmployeeLeaveBalances
        //                .FirstOrDefaultAsync(b => b.EmployeeId == employee.Id && b.LeaveTypeId == leaveType.Id);

        //            viewModels.Add(new AssignLeavesViewModel
        //            {
        //                EmployeeId = employee.Id,
        //                EmployeeName = employee.FullName,
        //                LeaveTypeId = leaveType.Id,
        //                LeaveTypeName = leaveType.Name,
        //                CurrentBalance = balance?.TotalLeaves ?? 0,
        //                DefaultLeavesAllowed = leaveType.LeavesAllowedPerYear
        //            });
        //        }
        //    }

        //    return View(viewModels);
        //}




        [HttpGet]
        public IActionResult DownloadHolidaySample()
        {
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Holidays");
                ws.Cell(1, 1).Value = "Date";
                ws.Cell(1, 2).Value = "Title";
                ws.Cell(2, 1).Value = "2024-01-26";
                ws.Cell(2, 2).Value = "Republic Day";
                ws.Cell(3, 1).Value = "2024-08-15";
                ws.Cell(3, 2).Value = "Independence Day";
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SampleHolidayUpload.xlsx");
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> BulkUploadHolidays(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var companyId = GetCurrentUserCompanyId();
            var holidays = new List<Holiday>();

            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var workbook = new XLWorkbook(stream))
                {
                    var ws = workbook.Worksheet(1);
                    foreach (var row in ws.RowsUsed().Skip(1))
                    {
                        if (DateTime.TryParse(row.Cell(1).GetString(), out var date))
                        {
                            holidays.Add(new Holiday
                            {
                                Date = date,
                                Title = row.Cell(2).GetString(),
                                CompanyId = companyId
                            });
                        }
                    }
                }
            }
            _context.Holidays.AddRange(holidays);
            await _context.SaveChangesAsync();
            return RedirectToAction("HolidayCalendar");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignLeaves()
        {
            var user = await _userManager.GetUserAsync(User);
            var companyId = user.CompanyId ?? 0;

            var employees = await _context.Employees
                .Where(e => e.CompanyId == companyId)
                .ToListAsync();

            var leaveTypes = await _context.LeaveTypes
                .Where(lt => lt.CompanyId == companyId)
                .ToListAsync();

            var viewModel = new AssignLeavesViewModel
            {
                Employees = employees,
                LeaveTypes = leaveTypes,
                Assignments = await _context.LeaveAssignments
                    .Where(la => la.IsActive)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Assign Leave to Employee
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignLeave(int employeeId, int leaveTypeId)
        {
            var existingAssignment = await _context.LeaveAssignments
                .FirstOrDefaultAsync(la => la.EmployeeId == employeeId &&
                                         la.LeaveTypeId == leaveTypeId &&
                                         la.IsActive);

            if (existingAssignment == null)
            {
                var assignment = new LeaveAssignment
                {
                    EmployeeId = employeeId,
                    LeaveTypeId = leaveTypeId,
                    IsActive = true,
                    StartDate = DateTime.UtcNow
                };

                _context.LeaveAssignments.Add(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AssignLeaves));
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignMultipleLeaves(List<int> employeeIds, List<int> leaveTypeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0 || leaveTypeIds == null || leaveTypeIds.Count == 0)
            {
                return Json(new { success = false, message = "No employees or leave types selected." });
            }

            try
            {
                foreach (var employeeId in employeeIds)
                {
                    foreach (var leaveTypeId in leaveTypeIds)
                    {
                        var existingAssignment = await _context.LeaveAssignments
                            .FirstOrDefaultAsync(la => la.EmployeeId == employeeId &&
                                                     la.LeaveTypeId == leaveTypeId &&
                                                     la.IsActive);

                        if (existingAssignment == null)
                        {
                            var assignment = new LeaveAssignment
                            {
                                EmployeeId = employeeId,
                                LeaveTypeId = leaveTypeId,
                                IsActive = true,
                                StartDate = DateTime.UtcNow
                            };
                            _context.LeaveAssignments.Add(assignment);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Leave rules assigned successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning leave rules to employees.");
                return Json(new { success = false, message = "An error occurred while assigning leave rules." });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BulkDeleteLeaveAssignments(List<int> employeeIds)
        {
            if (employeeIds == null || employeeIds.Count == 0)
            {
                return Json(new { success = false, message = "No employees selected." });
            }

            try
            {
                // Retrieve assignments, excluding those for Loss of Pay leave types
                var assignments = await _context.LeaveAssignments
                    .Include(la => la.LeaveType)
                    .Where(la => employeeIds.Contains(la.EmployeeId) && la.IsActive && !la.LeaveType.IsLossOfPay)
                    .ToListAsync();

                foreach (var assignment in assignments)
                {
                    assignment.IsActive = false;
                    assignment.EndDate = DateTime.UtcNow;
                    assignment.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Selected non-Loss of Pay leave assignments deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting leave assignments in bulk.");
                return Json(new { success = false, message = "An error occurred while deleting leave assignments." });
            }
        }

        // POST: Remove Leave Assignment
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveLeaveAssignment(int employeeId, int leaveTypeId)
        {
            var assignment = await _context.LeaveAssignments
                .FirstOrDefaultAsync(la => la.EmployeeId == employeeId &&
                                         la.LeaveTypeId == leaveTypeId &&
                                         la.IsActive);

            if (assignment != null)
            {
                assignment.IsActive = false;
                assignment.EndDate = DateTime.UtcNow;
                assignment.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AssignLeaves));
        }

        public async Task<IActionResult> WorkWeekRules()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.CompanyId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var companyId = user.CompanyId.Value;
            var workWeekRules = await _context.WorkWeekRules
                .Where(r => r.CompanyId == companyId)
                .ToListAsync();

            return View(workWeekRules);
        }

        public IActionResult CreateWorkWeekRule()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null || user.CompanyId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Pass CompanyId to the view if needed, or handle in POST
            // return View(new WorkWeekRule { CompanyId = user.CompanyId.Value });
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkWeekRule(WorkWeekRule workWeekRule)
        {
            // Remove validation errors related to the old boolean day properties
            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            {
                ModelState.Remove(day);
            }

            if (!ModelState.IsValid)
            {
                // We might need to re-populate the view model with the submitted WeeklyPatternJson
                return View(workWeekRule);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.CompanyId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            workWeekRule.CompanyId = user.CompanyId.Value;

            // If this rule is set as default, unset the previous default for this company
            if (workWeekRule.IsDefault)
            {
                var currentDefault = await _context.WorkWeekRules
                    .FirstOrDefaultAsync(r => r.CompanyId == workWeekRule.CompanyId && r.IsDefault);
                if (currentDefault != null)
                {
                    currentDefault.IsDefault = false;
                    _context.Update(currentDefault);
                }
            }

            _context.Add(workWeekRule);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(WorkWeekRules));
        }


        public async Task<IActionResult> EditWorkWeekRule(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.CompanyId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var workWeekRule = await _context.WorkWeekRules
                .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == user.CompanyId.Value);

            if (workWeekRule == null)
            {
                return NotFound();
            }

            return View(workWeekRule);
        }

        // ... existing code ...

        // POST: Edit Work Week Rule
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWorkWeekRule(int id, WorkWeekRule workWeekRule)
        {
            if (id != workWeekRule.Id)
            {
                return NotFound();
            }

            // Manually set CompanyId from user to prevent tampering
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.CompanyId == null)
            {
                return RedirectToAction("Index", "Home");
            }
            workWeekRule.CompanyId = user.CompanyId.Value;

            // Remove validation errors related to the old boolean day properties
            foreach (var day in new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" })
            {
                ModelState.Remove(day);
            }

            if (!ModelState.IsValid)
            {
                // We might need to re-populate the view model with the submitted WeeklyPatternJson
                return View(workWeekRule);
            }

            try
            {
                // If this rule is set as default, unset the previous default for this company
                if (workWeekRule.IsDefault)
                {
                    var currentDefault = await _context.WorkWeekRules
                        .FirstOrDefaultAsync(r => r.CompanyId == workWeekRule.CompanyId && r.IsDefault && r.Id != workWeekRule.Id);
                    if (currentDefault != null)
                    {
                        currentDefault.IsDefault = false;
                        _context.Update(currentDefault);
                    }
                }

                _context.Update(workWeekRule);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorkWeekRuleExists(workWeekRule.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToAction(nameof(WorkWeekRules));
        }



        [HttpPost]
        public async Task<IActionResult> DeleteWorkWeekRule(int id)
        {
            try
            {
                var rule = await _context.WorkWeekRules.FindAsync(id);
                if (rule == null)
                {
                    return Json(new { success = false, message = "Work week rule not found." });
                }

                // Check if this is the only default rule
                if (rule.IsDefault)
                {
                    var defaultRulesCount = await _context.WorkWeekRules
                        .Where(w => w.CompanyId == rule.CompanyId && w.IsDefault)
                        .CountAsync();

                    if (defaultRulesCount <= 1)
                    {
                        return Json(new { success = false, message = "Cannot delete the only default rule. Please set another rule as default first." });
                    }
                }

                _context.WorkWeekRules.Remove(rule);
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch 
            {
                return Json(new { success = false, message = "An error occurred while deleting the work week rule." });
            }
        }




        private bool WorkWeekRuleExists(int id)
        {
            return _context.WorkWeekRules.Any(e => e.Id == id);
        }




        private bool LeaveTypeExists(int id)
        {
            return _context.LeaveTypes.Any(e => e.Id == id);
        }

        public async Task<IActionResult> AssignWorkWeek()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null || user.CompanyId == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var companyId = user.CompanyId.Value;

            var employees = await _context.Employees
                .Where(e => e.CompanyId == companyId)
                .Include(e => e.WorkWeekRule) // Include the assigned work week rule
                .Select(e => new EmployeeWorkWeekViewModel
                {
                    Id = e.Id,
                    // Removed EmployeeCode
                    FullName = e.FullName,
                    Department = e.Department,
                    // Removed Location
                    // Removed Type
                    // Include the assigned work week rule name
                    AssignedWorkWeekRuleName = e.WorkWeekRule != null ? e.WorkWeekRule.Name : "Not Assigned"
                })
                .ToListAsync();

            var workWeekRules = await _context.WorkWeekRules
                .Where(r => r.CompanyId == companyId)
                .ToListAsync();

            var viewModel = new AssignWorkWeekViewModel
            {
                Employees = employees,
                WorkWeekRules = workWeekRules
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> AssignWorkWeekRule(List<int> employeeIds, int ruleId)
        {
            if (employeeIds == null || !employeeIds.Any() || ruleId <= 0)
            {
                return Json(new { success = false, message = "Invalid selection." });
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _context.Users.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || user.CompanyId == null)
                {
                    return Json(new { success = false, message = "User or company not found." });
                }

                var companyId = user.CompanyId.Value;

                // Verify the rule belongs to the user's company
                var workWeekRule = await _context.WorkWeekRules
                    .FirstOrDefaultAsync(r => r.Id == ruleId && r.CompanyId == companyId);

                if (workWeekRule == null)
                {
                    return Json(new { success = false, message = "Selected work week rule not found or does not belong to your company." });
                }

                // Fetch the employees belonging to the user's company from the selected IDs
                var employeesToUpdate = await _context.Employees
                    .Where(e => e.CompanyId == companyId && employeeIds.Contains(e.Id))
                    .ToListAsync();

                if (!employeesToUpdate.Any())
                {
                    return Json(new { success = false, message = "No selected employees found in your company." });
                }

                // Assign the work week rule to the selected employees
                foreach (var employee in employeesToUpdate)
                {
                    employee.WorkWeekRuleId = ruleId;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, ruleName = workWeekRule.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning work week rule.");
                return Json(new { success = false, message = "An error occurred while assigning the work week rule." });
            }
        }






    }
}
