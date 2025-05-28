using Microsoft.AspNetCore.Identity;
using Hrms_system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hrms_system.Models;
using Microsoft.EntityFrameworkCore; 



namespace Hrms_system.Controllers
{
    [Authorize]
    public class LeaveRequestController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public LeaveRequestController(ApplicationDbContext context, UserManager<ApplicationUser> userManager , ILogger<AccountController> logger    )
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
                return BadRequest(new
                {
                    Success = false,
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            try
            {
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

                decimal days = CalculateLeaveDays(
                    model.StartDate,
                    model.EndDate,
                    leaveType,
                    model.StartHalf,
                    model.EndHalf
                );

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

        // ... existing code ...
        // ... existing code ...
        private decimal CalculateLeaveDays(
            DateTime startDate,
            DateTime endDate,
            LeaveType leaveType,
            string startHalf,
            string endHalf)
        {
            // Case 1: Same date
            if (startDate.Date == endDate.Date)
            {
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

            // If leave starts with First Half and ends with Second Half: all days full
            if (startHalf == "First" && endHalf == "Second")
            {
                // Count all days, skipping weekends if policy says so
                for (int i = 0; i < totalDays; i++)
                {
                    var date = startDate.Date.AddDays(i);
                    if (!leaveType.ConsiderWeekendsBetweenLeave &&
                        (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                        continue;
                    days += 1m;
                }
                return days;
            }

            // If leave starts with First Half and ends with First Half: all days full except last (last is 0.5)
            if (startHalf == "First" && endHalf == "First")
            {
                for (int i = 0; i < totalDays - 1; i++)
                {
                    var date = startDate.Date.AddDays(i);
                    if (!leaveType.ConsiderWeekendsBetweenLeave &&
                        (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                        continue;
                    days += 1m;
                }
                // Last day (end date)
                var lastDate = endDate.Date;
                if (!leaveType.ConsiderWeekendsBetweenLeave &&
                    (lastDate.DayOfWeek == DayOfWeek.Saturday || lastDate.DayOfWeek == DayOfWeek.Sunday))
                {
                    // skip
                }
                else
                {
                    days += 0.5m;
                }
                return days;
            }

            // If leave starts with Second Half and ends with Second Half: first is 0.5, in-between full, last is 1
            if (startHalf == "Second" && endHalf == "Second")
            {
                // First day (start date)
                var firstDate = startDate.Date;
                if (!leaveType.ConsiderWeekendsBetweenLeave &&
                    (firstDate.DayOfWeek == DayOfWeek.Saturday || firstDate.DayOfWeek == DayOfWeek.Sunday))
                {
                    // skip
                }
                else
                {
                    days += 0.5m;
                }
                // In-between days
                for (int i = 1; i < totalDays - 1; i++)
                {
                    var date = startDate.Date.AddDays(i);
                    if (!leaveType.ConsiderWeekendsBetweenLeave &&
                        (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                        continue;
                    days += 1m;
                }
                // Last day (end date)
                var lastDate = endDate.Date;
                if (!leaveType.ConsiderWeekendsBetweenLeave &&
                    (lastDate.DayOfWeek == DayOfWeek.Saturday || lastDate.DayOfWeek == DayOfWeek.Sunday))
                {
                    // skip
                }
                else
                {
                    days += 1m;
                }
                return days;
            }

            // If leave starts with Second Half and ends with First Half: first is 0.5, in-between full, last is 0.5
            if (startHalf == "Second" && endHalf == "First")
            {
                // First day (start date)
                var firstDate = startDate.Date;
                if (!leaveType.ConsiderWeekendsBetweenLeave &&
                    (firstDate.DayOfWeek == DayOfWeek.Saturday || firstDate.DayOfWeek == DayOfWeek.Sunday))
                {
                    // skip
                }
                else
                {
                    days += 0.5m;
                }
                // In-between days
                for (int i = 1; i < totalDays - 1; i++)
                {
                    var date = startDate.Date.AddDays(i);
                    if (!leaveType.ConsiderWeekendsBetweenLeave &&
                        (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
                        continue;
                    days += 1m;
                }
                // Last day (end date)
                var lastDate = endDate.Date;
                if (!leaveType.ConsiderWeekendsBetweenLeave &&
                    (lastDate.DayOfWeek == DayOfWeek.Saturday || lastDate.DayOfWeek == DayOfWeek.Sunday))
                {
                    // skip
                }
                else
                {
                    days += 0.5m;
                }
                return days;
            }

            // Fallback (should not reach here)
            return days;
        }
        // ... existing code ...


        private decimal CalculateLeaveDays(DateTime startDate, DateTime endDate, LeaveType leaveType)
        {
            decimal days = 0;
            var currentDate = startDate;

            while (currentDate <= endDate)
            {
                //// Skip weekends if configured
                //if (!leaveType.ConsiderWeekendsBetweenLeave &&
                //    (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday))
                //{
                //    currentDate = currentDate.AddDays(1);
                //    continue;
                //}

                // Skip holidays if configured (you'll need a Holiday model)
                // if (!leaveType.ConsiderHolidaysBetweenLeave && IsHoliday(currentDate))
                // {
                //     currentDate = currentDate.AddDays(1);
                //     continue;
                // }

                days += 1;
                currentDate = currentDate.AddDays(1);
            }

            return days;
        }

        // Update ApproveLeave action to update used leaves
        [HttpPost]
        public async Task<IActionResult> ApproveLeave(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var leaveRequest = await _context.LeaveRequests
                    .Include(l => l.Employee)
                    .FirstOrDefaultAsync(l => l.Id == id);

                if (leaveRequest == null)
                {
                    return Json(new { success = false, message = "Leave request not found." });
                }

                var leaveBalance = await _context.EmployeeLeaveBalances
                    .FirstOrDefaultAsync(b =>
                        b.EmployeeId == leaveRequest.EmployeeId &&
                        b.LeaveTypeId == leaveRequest.LeaveTypeId);

                if (leaveBalance == null)
                {
                    return Json(new { success = false, message = "Leave balance not found." });
                }

                if (leaveBalance.AvailableLeaves < leaveRequest.Days)
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

                // Update leave request status and approval time
                leaveRequest.Status = "Approved";
                leaveRequest.ApprovedOn = DateTime.UtcNow;
                leaveRequest.RejectedOn = null;
                leaveRequest.RejectionReason = null;

                _context.Update(leaveBalance);
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
