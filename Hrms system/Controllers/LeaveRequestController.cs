using Microsoft.AspNetCore.Identity;
using Hrms_system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hrms_system.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using System.Text.Json;



namespace Hrms_system.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;


        public LeaveRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<AccountController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: LeaveRequest
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
            {
                return NotFound();
            }

            // Get leave requests
            var leaveRequests = await _context.LeaveRequests
                .Where(l => l.UserId == userId)
                .Include(l => l.User)
                .Include(l => l.LeaveType)
                .ToListAsync();

            // Get assigned leave types for this employee
            var assignedLeaveTypes = await _context.LeaveAssignments
                .Where(la => la.EmployeeId == employee.Id && la.IsActive)
                .Select(la => la.LeaveTypeId)
                .ToListAsync();

            var leaveTypes = await _context.LeaveTypes
                .Where(lt => assignedLeaveTypes.Contains(lt.Id))
                .ToListAsync();

            ViewBag.LeaveTypes = leaveTypes;
            return View(leaveRequests);
        }


        private async Task<bool> IsOffDay(DateTime date, int employeeId, bool considerWeekends)
        {
            var employee = await _context.Employees
                .Include(e => e.WorkWeekRule)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee?.WorkWeekRule == null)
            {
                _logger.LogWarning($"No work week rule found for employee {employeeId}");
                return !considerWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
            }

            if (string.IsNullOrEmpty(employee.WorkWeekRule.WeeklyPatternJson))
            {
                _logger.LogWarning($"Empty WeeklyPatternJson for employee {employeeId}");
                return !considerWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
            }

            try
            {
                var weeklyPattern = System.Text.Json.JsonSerializer.Deserialize<int[][]>(employee.WorkWeekRule.WeeklyPatternJson);
                if (weeklyPattern == null || weeklyPattern.Length == 0)
                {
                    _logger.LogWarning($"Invalid WeeklyPatternJson for employee {employeeId}");
                    return !considerWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
                }

                // Get the week number (1-5) for the date
                int weekNumber = ((date.Day - 1) / 7) + 1;
                if (weekNumber > 5) weekNumber = 5;

                // Get the day of week (0-6, where 0 is Monday)
                int dayOfWeek = ((int)date.DayOfWeek + 6) % 7;

                _logger.LogInformation($"Date: {date:yyyy-MM-dd}, Week: {weekNumber}, Day: {dayOfWeek}, Pattern: {employee.WorkWeekRule.WeeklyPatternJson}");

                // Check if the day is marked as off in the pattern (0 means off day, 1 means working day)
                bool isOffDay = weekNumber <= weeklyPattern.Length &&
                               dayOfWeek < weeklyPattern[weekNumber - 1].Length &&
                               weeklyPattern[weekNumber - 1][dayOfWeek] == 0;

                _logger.LogInformation($"IsOffDay: {isOffDay} for date {date:yyyy-MM-dd}");

                return isOffDay;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"Error deserializing WeeklyPatternJson for employee {employeeId}");
                return !considerWeekends && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
            }
        }




        // GET: LeaveRequest/Create
        public IActionResult Create()
        {
            return View();
        }
        // Update the Create action in LeaveRequestController

        [HttpPost]
        [Route("LeaveRequest/Create")]
        public async Task<IActionResult> Create([FromBody] CreateLeaveRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                // Add more descriptive error messages
                if (errors.Contains("The model field is required."))
                {
                    errors.Add("Please fill in all required fields.");
                }
                if (errors.Contains("The JSON value could not be converted to System.Int32. Path: $.LeaveTypeId"))
                {
                    errors.Add("Please select a valid leave type.");
                }

                return BadRequest(new
                {
                    Success = false,
                    Errors = errors
                });
            }

            try
            {
                if (model.StartDate.Date == model.EndDate.Date)
                {
                    if (model.StartHalf == "Second" && model.EndHalf == "First")
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Error = "Invalid combination: Cannot have Second Half before First Half on the same day."
                        });
                    }
                }
                var userId = _userManager.GetUserId(User);
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                {
                    return BadRequest(new { Success = false, Error = "Employee not found" });
                }

                // Verify that the leave type is assigned to the employee
                var isLeaveTypeAssigned = await _context.LeaveAssignments
                    .AnyAsync(la => la.EmployeeId == employee.Id &&
                                  la.LeaveTypeId == model.LeaveTypeId &&
                                  la.IsActive);

                if (!isLeaveTypeAssigned)
                {
                    return BadRequest(new { Success = false, Error = "This leave type is not assigned to you" });
                }

                var leaveType = await _context.LeaveTypes
                    .FirstOrDefaultAsync(lt => lt.Id == model.LeaveTypeId);

                if (leaveType == null)
                {
                    return BadRequest(new { Success = false, Error = "Invalid leave type" });
                }

                if (model.StartDate > model.EndDate)
                {
                    return BadRequest(new { Success = false, Error = "End date must be after start date" });
                }

                if (model.StartHalf != "First" && model.StartHalf != "Second")
                {
                    return BadRequest(new { Success = false, Error = "Invalid start half selection" });
                }
                if (model.EndHalf != "First" && model.EndHalf != "Second")
                {
                    return BadRequest(new { Success = false, Error = "Invalid end half selection" });
                }

                // Calculate leave days considering the ConsiderWeekendsBetweenLeave setting
                decimal days = CalculateLeaveDays(
                    model.StartDate,
                    model.EndDate,
                    leaveType,
                    model.StartHalf,
                    model.EndHalf
                );

                // For Loss of Pay leaves, we don't need to check leave balance
                if (!leaveType.IsLossOfPay)
                {
                    var balance = await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == employee.Id &&
                                                b.LeaveTypeId == model.LeaveTypeId &&
                                                b.Year == DateTime.UtcNow.Year);

                    if (balance == null)
                    {
                        return BadRequest(new { Success = false, Error = "No leave balance found for this leave type" });
                    }

                    decimal availableLeaves = balance.TotalLeaves - balance.UsedLeaves - balance.PendingLeaves;
                    if (availableLeaves < days)
                    {
                        return BadRequest(new
                        {
                            Success = false,
                            Error = $"Insufficient leave balance. You have {availableLeaves} days available, but requested {days} days."
                        });
                    }
                }

                var leaveRequest = new LeaveRequest
                {
                    UserId = userId,
                    EmployeeId = employee.Id,
                    CompanyId = employee.CompanyId,
                    LeaveTypeId = model.LeaveTypeId,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    Reason = model.Reason,
                    AppliedOn = DateTime.UtcNow,
                    Status = "Pending",
                    StartHalf = model.StartHalf,
                    EndHalf = model.EndHalf,
                    Days = days
                };

                _context.LeaveRequests.Add(leaveRequest);

                // For Loss of Pay leaves, we don't need to update leave balance
                if (!leaveType.IsLossOfPay)
                {
                    var balance = await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(b => b.EmployeeId == employee.Id &&
                                                b.LeaveTypeId == model.LeaveTypeId &&
                                                b.Year == DateTime.UtcNow.Year);

                    if (balance != null)
                    {
                        balance.PendingLeaves += days;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    Success = true,
                    Message = "Leave request submitted successfully",
                    Data = new
                    {
                        leaveRequest.Id,
                        leaveRequest.Status,
                        leaveRequest.Days
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating leave request");
                return StatusCode(500, new
                {
                    Success = false,
                    Error = "An error occurred",
                    Details = ex.Message
                });
            }
        }




        private decimal CalculateLeaveDays(
                   DateTime startDate,
                   DateTime endDate,
                   LeaveType leaveType,
                   string startHalf,
                   string endHalf)
        {
            // Get employee ID from the current user
            var userId = _userManager.GetUserId(User);
            var employee = _context.Employees.FirstOrDefault(e => e.UserId == userId);
            if (employee == null) return 0;

            // Case 1: Same date
            if (startDate.Date == endDate.Date)
            {
                // Check if it's an off day
                var isOffDay = IsOffDay(startDate, employee.Id, leaveType.ConsiderWeekendsBetweenLeave).Result;
                if (isOffDay)
                {
                    return 0m; // Return 0 days for off days
                }

                if (startHalf == endHalf)
                {
                    // Both halves same: half-day leave
                    return 0.5m;
                }
                else
                {
                    // Different halves: full day leave
                    return 1m;
                }
            }

            // Case 2: Multiple days
            decimal days = 0;
            int totalDays = (endDate.Date - startDate.Date).Days + 1;

            // If ConsiderWeekendsBetweenLeave is true, count all days
            if (leaveType.ConsiderWeekendsBetweenLeave)
            {
                // If leave starts with First Half and ends with Second Half: all days full
                if (startHalf == "First" && endHalf == "Second")
                {
                    return totalDays;
                }
                // If leave starts with First Half and ends with First Half: all days full except last (last is 0.5)
                else if (startHalf == "First" && endHalf == "First")
                {
                    return totalDays - 0.5m;
                }
                // If leave starts with Second Half and ends with Second Half: first is 0.5, in-between full, last is 1
                else if (startHalf == "Second" && endHalf == "Second")
                {
                    return totalDays - 0.5m;
                }
                // If leave starts with Second Half and ends with First Half: first is 0.5, in-between full, last is 0.5
                else if (startHalf == "Second" && endHalf == "First")
                {
                    return totalDays - 1m;
                }
            }
            // If ConsiderWeekendsBetweenLeave is false, check work week rules
            else
            {
                // If leave starts with First Half and ends with Second Half: all days full
                if (startHalf == "First" && endHalf == "Second")
                {
                    for (int i = 0; i < totalDays; i++)
                    {
                        var date = startDate.Date.AddDays(i);
                        var isOffDay = IsOffDay(date, employee.Id, false).Result;
                        if (!isOffDay)
                        {
                            days += 1m;
                        }
                    }
                    return days;
                }
                // If leave starts with First Half and ends with First Half: all days full except last (last is 0.5)
                else if (startHalf == "First" && endHalf == "First")
                {
                    for (int i = 0; i < totalDays - 1; i++)
                    {
                        var date = startDate.Date.AddDays(i);
                        var isOffDay = IsOffDay(date, employee.Id, false).Result;
                        if (!isOffDay)
                        {
                            days += 1m;
                        }
                    }
                    // Last day (end date)
                    var lastDate = endDate.Date;
                    var isLastDayOff = IsOffDay(lastDate, employee.Id, false).Result;
                    if (!isLastDayOff)
                    {
                        days += 0.5m;
                    }
                    return days;
                }
                // If leave starts with Second Half and ends with Second Half: first is 0.5, in-between full, last is 1
                else if (startHalf == "Second" && endHalf == "Second")
                {
                    // First day (start date)
                    var firstDate = startDate.Date;
                    var isFirstDayOff = IsOffDay(firstDate, employee.Id, false).Result;
                    if (!isFirstDayOff)
                    {
                        days += 0.5m;
                    }
                    // In-between days
                    for (int i = 1; i < totalDays - 1; i++)
                    {
                        var date = startDate.Date.AddDays(i);
                        var isOffDay = IsOffDay(date, employee.Id, false).Result;
                        if (!isOffDay)
                        {
                            days += 1m;
                        }
                    }
                    // Last day (end date)
                    var lastDate = endDate.Date;
                    var isLastDayOff = IsOffDay(lastDate, employee.Id, false).Result;
                    if (!isLastDayOff)
                    {
                        days += 1m;
                    }
                    return days;
                }
                // If leave starts with Second Half and ends with First Half: first is 0.5, in-between full, last is 0.5
                else if (startHalf == "Second" && endHalf == "First")
                {
                    // First day (start date)
                    var firstDate = startDate.Date;
                    var isFirstDayOff = IsOffDay(firstDate, employee.Id, false).Result;
                    if (!isFirstDayOff)
                    {
                        days += 0.5m;
                    }
                    // In-between days
                    for (int i = 1; i < totalDays - 1; i++)
                    {
                        var date = startDate.Date.AddDays(i);
                        var isOffDay = IsOffDay(date, employee.Id, false).Result;
                        if (!isOffDay)
                        {
                            days += 1m;
                        }
                    }
                    // Last day (end date)
                    var lastDate = endDate.Date;
                    var isLastDayOff = IsOffDay(lastDate, employee.Id, false).Result;
                    if (!isLastDayOff)
                    {
                        days += 0.5m;
                    }
                    return days;
                }
            }

            // Fallback (should not reach here)
            return days;
        }



        //private decimal CalculateLeaveDays(DateTime startDate, DateTime endDate, LeaveType leaveType)
        //{
        //    decimal days = 0;
        //    var currentDate = startDate;

        //    while (currentDate <= endDate)
        //    {
        //        //// Skip weekends if configured
        //        //if (!leaveType.ConsiderWeekendsBetweenLeave &&
        //        //    (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
        //        //{
        //        //    currentDate = currentDate.AddDays(1);
        //        //    continue;
        //        //}

        //        // Skip holidays if configured (you'll need a Holiday model)
        //        // if (!leaveType.ConsiderHolidaysBetweenLeave && IsHoliday(currentDate))
        //        // {
        //        //     currentDate = currentDate.AddDays(1);
        //        //     continue;
        //        // }

        //        days += 1;
        //        currentDate = currentDate.AddDays(1);
        //    }

        //    return days;
        //}

        // Update ApproveLeave action to update used leaves
        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .Include(l => l.LeaveType)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (leaveRequest == null)
                {
                    return Json(new { success = false, message = "Leave request not found." });
                }
                if (leaveRequest.LeaveType == null)
                {
                    return Json(new { success = false, message = "Leave type not found for this request." });
                }

                // Skip balance check for loss of pay leaves
                if (!leaveRequest.LeaveType.IsLossOfPay)
                {
                    var leaveBalance = await _context.EmployeeLeaveBalances
                        .FirstOrDefaultAsync(b =>
                            b.EmployeeId == leaveRequest.EmployeeId &&
                            b.LeaveTypeId == leaveRequest.LeaveTypeId &&
                            b.Year == DateTime.UtcNow.Year);

                    if (leaveBalance == null)
                    {
                        return Json(new { success = false, message = "Leave balance not found." });
                    }

                    // Calculate available leaves correctly
                    decimal availableLeaves = leaveBalance.TotalLeaves - leaveBalance.UsedLeaves - leaveBalance.PendingLeaves + leaveRequest.Days;
                    if (availableLeaves < leaveRequest.Days)
                    {
                        return Json(new { success = false, message = "Not enough available leaves." });
                    }

                    // Update balances
                    leaveBalance.PendingLeaves -= leaveRequest.Days;
                    leaveBalance.UsedLeaves += leaveRequest.Days;

                    // Ensure no negative values
                    if (leaveBalance.PendingLeaves < 0)
                    {
                        _logger.LogWarning($"Negative pending leaves detected for balance ID {leaveBalance.Id}");
                        leaveBalance.PendingLeaves = 0;
                    }

                    _context.Update(leaveBalance);
                }

                // Update leave request status and approval time
                leaveRequest.Status = "Approved";
                leaveRequest.ApprovedOn = DateTime.UtcNow;
                leaveRequest.RejectedOn = null;
                leaveRequest.RejectionReason = null;

                _context.Update(leaveRequest);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error approving leave");
                return Json(new { success = false, message = "An error occurred while approving the leave." });
            }
        }




        [HttpPost]
        public async Task<IActionResult> RejectLeave(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var leave = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (leave == null) return NotFound();

                var leaveBalance = await _context.EmployeeLeaveBalances
                    .FirstOrDefaultAsync(b => b.EmployeeId == leave.EmployeeId && b.LeaveTypeId == leave.LeaveTypeId);

                if (leaveBalance != null)
                {
                    // Handle based on current status
                    if (leave.Status == "Pending")
                    {
                        leaveBalance.PendingLeaves -= leave.Days;
                    }
                    else if (leave.Status == "Approved")
                    {
                        // Revert UsedLeaves
                        leaveBalance.UsedLeaves -= leave.Days;
                    }

                    // Ensure no negative values
                    if (leaveBalance.PendingLeaves < 0) leaveBalance.PendingLeaves = 0;
                    if (leaveBalance.UsedLeaves < 0) leaveBalance.UsedLeaves = 0;

                    _context.Update(leaveBalance);
                }

                leave.Status = "Rejected";
                leave.RejectedOn = DateTime.UtcNow;
                leave.ApprovedOn = null;
                leave.RejectionReason = "Rejected by admin"; // Optional: can be passed as a parameter

                _context.Update(leave);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error rejecting leave");
                return StatusCode(500, "Error rejecting leave");
            }
        }


        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] LeaveRequest model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(new { errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        //    }

        //    var userId = _userManager.GetUserId(User);

        //    // 🔍 Find the employee to get the CompanyId
        //    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
        //    if (employee == null)
        //    {
        //        return BadRequest(new { error = "Employee profile not found." });
        //    }

        //    model.UserId = userId;
        //    model.EmployeeId = employee.Id;
        //    model.CompanyId = employee.CompanyId;
        //    model.AppliedOn = DateTime.UtcNow;
        //    model.Status = "Pending";

        //    _context.LeaveRequests.Add(model);
        //    await _context.SaveChangesAsync();


        //    return Ok(new { message = "Leave request submitted successfully!" });
        //}



        //[HttpPost]
        //public async Task<IActionResult> ApproveLeave(int id)
        //{
        //    var leave = await _context.LeaveRequests.FindAsync(id);
        //    if (leave == null) return NotFound();

        //    leave.Status = "Approved";
        //    _context.Update(leave);
        //    await _context.SaveChangesAsync();
        //    return Json(new { success = true });
        //}

        //[HttpPost]
        //public async Task<IActionResult> RejectLeave(int id)
        //{
        //    var leave = await _context.LeaveRequests.FindAsync(id);
        //    if (leave == null) return NotFound();

        //    leave.Status = "Rejected";
        //    _context.Update(leave);
        //    await _context.SaveChangesAsync();
        //    return Json(new { success = true });
        //}


        [HttpGet]
        public async Task<IActionResult> GetLeaveDetails(int id)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(l => l.Employee)
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (leaveRequest == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                id = leaveRequest.Id,
                employeeName = leaveRequest.Employee?.FullName,
                position = leaveRequest.Employee?.Position,
                leaveType = leaveRequest.LeaveType,
                startDate = leaveRequest.StartDate.ToString("dd MMM yyyy"),
                endDate = leaveRequest.EndDate.ToString("dd MMM yyyy"),
                days = leaveRequest.Days,
                reason = leaveRequest.Reason,
                status = leaveRequest.Status,
                appliedOn = leaveRequest.AppliedOn.ToString("dd MMM yyyy"),
            });
        }

        // In LeaveRequestController
        public async Task<IActionResult> MyLeaveBalances()
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
            {
                return NotFound();
            }

            // Get assigned leave types for this employee
            var assignedLeaveTypeIds = await _context.LeaveAssignments
                .Where(la => la.EmployeeId == employee.Id && la.IsActive)
                .Select(la => la.LeaveTypeId)
                .ToListAsync();

            // Get leave balances only for assigned leave types
            var balances = await _context.EmployeeLeaveBalances
                .Include(b => b.LeaveType)
                .Where(b => b.EmployeeId == employee.Id && assignedLeaveTypeIds.Contains(b.LeaveTypeId))
                .ToListAsync();

            return View(balances);
        }


    }
}
