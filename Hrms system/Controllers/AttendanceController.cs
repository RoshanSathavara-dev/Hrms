using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Hrms_system.Data;
using Hrms_system.Models;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using System.IO;
using Microsoft.IdentityModel.Tokens;

namespace Hrms_system.Controllers
{
    [Authorize]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AttendanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var attendanceRecords = await _context.Attendance
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.ClockIn)
                .ToListAsync();

            return View(attendanceRecords);
        }

        [HttpGet]
        public async Task<IActionResult> GetAttendanceList()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == user.Id);
            if (employee == null) return Unauthorized();

            var attendanceList = await _context.Attendance
                .Include(a => a.Employee)
                .Where(a => a.CompanyId == employee.CompanyId)
                .OrderByDescending(a => a.ClockIn)
                .ToListAsync();


            var result = attendanceList.Select(a => new {
                EmployeeId = a.Employee?.Id,
                Name = $"{a.Employee?.FirstName} {a.Employee?.LastName}",
                Department = a.Employee?.Department,
                ClockIn = a.ClockIn.ToString("hh:mm tt"),
                ClockOut = a.ClockOut.HasValue ? a.ClockOut.Value.ToString("hh:mm tt") : "-",
                Status = a.ClockOut != null ? "Present" : a.IsOnBreak ? "On Break" : "Pending"
            });

            return Json(result);
        }

        [HttpGet]
        public IActionResult FilterAttendance(DateTime? fromDate, DateTime? toDate, string? department)
        {
            int? companyId = HttpContext.Session.GetInt32("CompanyId");
            if (companyId == null)
            {
                return Unauthorized();
            }

            var query = _context.Attendance
                .Include(a => a.Employee)
                .Where(a => a.Employee != null && a.Employee.CompanyId == companyId);

            if (fromDate.HasValue)
            {
                query = query.Where(a => a.ClockIn.Date >= fromDate.Value.Date);
            }

            if (toDate.HasValue)
            {
                query = query.Where(a => a.ClockIn.Date <= toDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(department))
            {
                query = query.Where(a => a.Employee!.Department == department); // '!' tells compiler it's not null
            }

            var result = query.Select(a => new
            {
                employeeId = a.Employee!.Id,
                firstName = a.Employee!.FirstName,
                lastName = a.Employee!.LastName,
                department = a.Employee!.Department,
                clockIn = a.ClockIn,
                clockOut = a.ClockOut,
                isOnBreak = a.IsOnBreak
            }).ToList();

            return Json(result);
        }





        [HttpGet]
        public async Task<IActionResult> GetAttendanceStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var attendance = await _context.Attendance
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.ClockIn.Date == DateTime.Today);

            if (attendance == null)
            {
                return Ok(new { isClockedIn = false });
            }

            if (attendance.ClockOut.HasValue)
            {
                return Ok(new { isClockedIn = false });
            }

            // ✅ Fix: Ensure `totalBreak` never goes negative
            TimeSpan totalBreak = attendance.TotalBreakDuration;
            if (attendance.BreakStart.HasValue && !attendance.BreakEnd.HasValue)
            {
                var ongoingBreak = DateTime.UtcNow - attendance.BreakStart.Value;
                if (ongoingBreak.TotalMilliseconds > 0)  // ✅ Ensure non-negative value
                {
                    totalBreak += ongoingBreak;
                }
            }

            return Ok(new
            {
                isClockedIn = true,
                clockInTime = attendance.ClockIn,
                isOnBreak = attendance.BreakStart.HasValue && !attendance.BreakEnd.HasValue,
                breakStartTime = attendance.BreakStart,
                totalBreakDuration = Math.Max(0, totalBreak.TotalMilliseconds)  // ✅ Prevent negative values
            });
        }







        [HttpPost]
        public async Task<IActionResult> ClockIn()
        {
            // Get current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "Unauthorized access." });

            // Find the employee record associated with this user
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == user.Id);

            if (employee == null)
            {
                return BadRequest(new { message = "No employee record found for this user." });
            }

            // Convert to Indian Standard Time
            TimeZoneInfo indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);

            var attendance = new Attendance
            {
                UserId = user.Id,
                EmployeeId = employee.Id,
                CompanyId = employee.CompanyId,
                ClockIn = istNow,
                CreatedBy = employee.Id // 👈 This is the Employee's integer ID                                        // 👈 Add CreatedBy here
            };

            _context.Attendance.Add(attendance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                isClockedIn = true,
                clockInTime = attendance.ClockIn.ToString("o"),
                isOnBreak = false,
                employeeId = employee.Id
            });
        }



        // ✅ Clock Out
        [HttpPost]
        public async Task<IActionResult> ClockOut()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var attendance = await _context.Attendance
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.ClockIn.Date == DateTime.Today);

            if (attendance == null || attendance.ClockOut.HasValue)
            {
                return BadRequest(new { message = "You have already clocked out." });
            }

            // ✅ Convert UTC to IST
            TimeZoneInfo indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);

            // ✅ Save clock out time
            attendance.ClockOut = istTime;
            _context.Attendance.Update(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { clockOutTime = istTime.ToString("o"), isClockedIn = false });
        }





        [HttpPost]
        public async Task<IActionResult> StartBreak()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized(new { message = "User not found or unauthorized." });

            var today = DateTime.UtcNow.Date;
            var attendance = await _context.Attendance
                .Include(a => a.BreakLogs)
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.ClockIn.Date == today);

            if (attendance == null)
            {
                return BadRequest(new { message = "No active attendance record found for today. Please clock in first." });
            }

            if (attendance.ClockOut != null)
            {
                return BadRequest(new { message = "You have already clocked out. Cannot start a break." });
            }

            // Fetch policy for the user's company
            var policy = await _context.AttendancePolicies.FirstOrDefaultAsync(p => p.CompanyId == attendance.CompanyId);
            int maxBreaks = policy?.MaxBreaksPerDay ?? 2;

            // Count breaks today
            int breaksTaken = attendance.BreakLogs.Count(bl => bl.BreakStart.Date == today);
            if (breaksTaken >= maxBreaks)
            {
                return BadRequest(new { message = "You have already taken a break today." });
            }

            // Check if already on a break
            if (attendance.BreakLogs.Any(bl => bl.BreakEnd == null))
            {
                return BadRequest(new { message = "You are already on a break." });
            }

            TimeZoneInfo indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);

            // Create new BreakLog
            var breakLog = new BreakLog
            {
                AttendanceId = attendance.Id,
                BreakStart = istTime,
                BreakEnd = null
            };
            attendance.BreakLogs.Add(breakLog);
            attendance.BreakCount = breaksTaken + 1;
            attendance.BreakStart = istTime;
            attendance.BreakEnd = null;

            _context.Attendance.Update(attendance);
            await _context.SaveChangesAsync();

            return Ok(new { breakStartTime = istTime.ToString("o") });
        }
        // ... existing code ...
        [HttpPost]
        public async Task<IActionResult> EndBreak()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var today = DateTime.UtcNow.Date;
            var attendance = await _context.Attendance
                .Include(a => a.BreakLogs)
                .FirstOrDefaultAsync(a => a.UserId == user.Id && a.ClockIn.Date == today);

            if (attendance == null)
            {
                return BadRequest(new { message = "No active attendance record found for today." });
            }

            // Find the open break log
            var openBreak = attendance.BreakLogs.FirstOrDefault(bl => bl.BreakEnd == null);
            if (openBreak == null)
            {
                return BadRequest(new { message = "No active break found." });
            }

            TimeZoneInfo indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, indianTimeZone);

            openBreak.BreakEnd = istTime;
            attendance.BreakEnd = istTime;
            if (attendance.BreakStart.HasValue)
            {
                attendance.TotalBreakDuration += istTime - attendance.BreakStart.Value;
            }
            attendance.BreakStart = null;

            _context.Attendance.Update(attendance);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                breakEndTime = istTime.ToString("o"),
                totalBreakDuration = attendance.TotalBreakDuration.TotalMilliseconds,
                isOnBreak = false
            });
        }

        private TimeSpan CalculateRemainingTime(DateTime clockIn, DateTime? clockOut, TimeSpan totalBreakDuration)
        {
            TimeSpan requiredWorkHours = TimeSpan.FromHours(8); // Assuming 8 hours of work
            TimeSpan workedHours = (clockOut ?? DateTime.Now) - clockIn - totalBreakDuration;

            return requiredWorkHours - workedHours;
        }
        public async Task<IActionResult> AttendanceLog(DateTime? date, string viewType = "daily", string sortOrder = "desc")
        {
            date ??= DateTime.Today;
            var userId = _userManager.GetUserId(User);
            List<DailyAttendanceViewModel> model = new List<DailyAttendanceViewModel>();

            if (viewType == "monthly")
            {
                var startDate = new DateTime(date.Value.Year, date.Value.Month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                var today = DateTime.Today;

                var records = await _context.Attendance
                    .Include(a => a.BreakLogs)
                    .Where(a => a.UserId == userId && a.ClockIn.Date >= startDate && a.ClockIn.Date <= endDate)
                    .OrderBy(a => a.ClockIn)
                    .ToListAsync();

                // Get all days in the month
                var allDays = Enumerable.Range(1, DateTime.DaysInMonth(date.Value.Year, date.Value.Month))
                    .Select(day => new DateTime(date.Value.Year, date.Value.Month, day))
                    .ToList();

                // Split into two parts - today and before, and after today
                var todayAndBefore = allDays.Where(d => d <= today).OrderByDescending(d => d);
                var afterToday = allDays.Where(d => d > today).OrderByDescending(d => d);

                // Combine based on sort order
                model = (sortOrder == "desc" ? todayAndBefore.Concat(afterToday) : allDays.OrderBy(d => d))
                    .Select(d =>
                    {
                        var record = records.FirstOrDefault(a => a.ClockIn.Date == d);
                        return record != null
                            ? new DailyAttendanceViewModel
                            {
                                Date = d,
                                ClockInTime = record.ClockIn.TimeOfDay,
                                ClockOutTime = record.ClockOut?.TimeOfDay,
                                Breaks = record.BreakLogs
                                    .Where(bl => bl.BreakStart.Date == d && bl.BreakEnd.HasValue)
                                    .OrderBy(bl => bl.BreakStart)
                                    .Select(bl => new BreakPeriodViewModel
                                    {
                                        StartTime = bl.BreakStart.TimeOfDay,
                                        EndTime = bl.BreakEnd!.Value.TimeOfDay
                                    }).ToList(),
                                TotalWorkedHours = (record.ClockOut ?? DateTime.Now) - record.ClockIn - record.TotalBreakDuration,
                                TotalBreakHours = record.TotalBreakDuration
                            }
                            : new DailyAttendanceViewModel
                            {
                                Date = d,
                                ClockInTime = TimeSpan.Zero,
                                ClockOutTime = null,
                                Breaks = new List<BreakPeriodViewModel>(),
                                TotalWorkedHours = TimeSpan.Zero,
                                TotalBreakHours = TimeSpan.Zero
                            };
                    }).ToList();
            }
            else
            {
                var records = await _context.Attendance
                    .Include(a => a.BreakLogs)
                    .Where(a => a.UserId == userId && a.ClockIn.Date == date.Value.Date)
                    .OrderByDescending(a => a.ClockIn)
                    .ToListAsync();

                model = records.Select(a => new DailyAttendanceViewModel
                {
                    Date = a.ClockIn.Date,
                    ClockInTime = a.ClockIn.TimeOfDay,
                    ClockOutTime = a.ClockOut?.TimeOfDay,
                    Breaks = a.BreakLogs
                        .Where(bl => bl.BreakStart.Date == a.ClockIn.Date && bl.BreakEnd.HasValue)
                        .OrderBy(bl => bl.BreakStart)
                        .Select(bl => new BreakPeriodViewModel
                        {
                            StartTime = bl.BreakStart.TimeOfDay,
                            EndTime = bl.BreakEnd!.Value.TimeOfDay
                        }).ToList(),
                    TotalWorkedHours = (a.ClockOut ?? DateTime.Now) - a.ClockIn - a.TotalBreakDuration,
                    TotalBreakHours = a.TotalBreakDuration
                }).ToList();

                // Ensure we have at least one record for the day
                if (!model.Any())
                {
                    model.Add(new DailyAttendanceViewModel
                    {
                        Date = date.Value,
                        ClockInTime = TimeSpan.Zero,
                        ClockOutTime = null,
                        Breaks = new List<BreakPeriodViewModel>(),
                        TotalWorkedHours = TimeSpan.Zero,
                        TotalBreakHours = TimeSpan.Zero
                    });
                }
            }

            ViewBag.SelectedDate = date.Value;
            ViewBag.ViewType = viewType;
            ViewBag.SortOrder = sortOrder;
            return View(model);
        }

        //     public async Task<IActionResult> AttendanceLog(DateTime? date, string viewType = "daily", string sortOrder = "desc")
        //     {
        //         date ??= DateTime.Today;
        //         var userId = _userManager.GetUserId(User);
        //         List<DailyAttendanceViewModel> model = new List<DailyAttendanceViewModel>();

        //         if (viewType == "monthly")
        //         {
        //             var startDate = new DateTime(date.Value.Year, date.Value.Month, 1);
        //             var endDate = startDate.AddMonths(1).AddDays(-1);
        //             var today = DateTime.Today;

        //             var records = await _context.Attendance
        //                 .Where(a => a.UserId == userId && a.ClockIn.Date >= startDate && a.ClockIn.Date <= endDate)
        //                 .OrderBy(a => a.ClockIn)
        //                 .ToListAsync();

        //             // Get all days in the month
        //             var allDays = Enumerable.Range(1, DateTime.DaysInMonth(date.Value.Year, date.Value.Month))
        //                 .Select(day => new DateTime(date.Value.Year, date.Value.Month, day))
        //                 .ToList();

        //             // Split into two parts - today and before, and after today
        //             var todayAndBefore = allDays.Where(d => d <= today).OrderByDescending(d => d);
        //             var afterToday = allDays.Where(d => d > today).OrderByDescending(d => d);

        //             // Combine based on sort order
        //             model = (sortOrder == "desc" ? todayAndBefore.Concat(afterToday) : allDays.OrderBy(d => d))
        //                 .Select(d =>
        //                 {
        //                     var record = records.FirstOrDefault(a => a.ClockIn.Date == d);
        //                     return record != null
        //                         ? new DailyAttendanceViewModel
        //                         {
        //                             Date = d,
        //                             ClockInTime = record.ClockIn.TimeOfDay,
        //                             ClockOutTime = record.ClockOut?.TimeOfDay,
        //                             Breaks = record.BreakStart.HasValue && record.BreakEnd.HasValue
        //                                 ? new List<BreakPeriodViewModel> { new BreakPeriodViewModel
        //                         {
        //                             StartTime = record.BreakStart.Value.TimeOfDay,
        //                             EndTime = record.BreakEnd.Value.TimeOfDay
        //                         }}
        //                                 : new List<BreakPeriodViewModel>(),
        //                             TotalWorkedHours = (record.ClockOut ?? DateTime.Now) - record.ClockIn - record.TotalBreakDuration,
        //                             TotalBreakHours = record.TotalBreakDuration
        //                         }
        //                         : new DailyAttendanceViewModel
        //                         {
        //                             Date = d,
        //                             ClockInTime = TimeSpan.Zero,
        //                             ClockOutTime = null,
        //                             Breaks = new List<BreakPeriodViewModel>(),
        //                             TotalWorkedHours = TimeSpan.Zero,
        //                             TotalBreakHours = TimeSpan.Zero
        //                         };
        //                 }).ToList();
        //         }
        //         else
        //         {
        //             var records = await _context.Attendance
        //  .Where(a => a.UserId == userId && a.ClockIn.Date == date.Value.Date)
        //  .OrderByDescending(a => a.ClockIn)
        //  .ToListAsync();

        //             model = records.Select(a => new DailyAttendanceViewModel
        //             {
        //                 Date = a.ClockIn.Date,
        //                 ClockInTime = a.ClockIn.TimeOfDay,
        //                 ClockOutTime = a.ClockOut?.TimeOfDay,
        //                 Breaks = a.BreakStart.HasValue && a.BreakEnd.HasValue
        //                     ? new List<BreakPeriodViewModel> { new BreakPeriodViewModel
        //{
        //    StartTime = a.BreakStart.Value.TimeOfDay,
        //    EndTime = a.BreakEnd.Value.TimeOfDay
        //}}
        //                     : new List<BreakPeriodViewModel>(),
        //                 TotalWorkedHours = (a.ClockOut ?? DateTime.Now) - a.ClockIn - a.TotalBreakDuration,
        //                 TotalBreakHours = a.TotalBreakDuration
        //             }).ToList();

        //             // Ensure we have at least one record for the day
        //             if (!model.Any())
        //             {
        //                 model.Add(new DailyAttendanceViewModel
        //                 {
        //                     Date = date.Value,
        //                     ClockInTime = TimeSpan.Zero,
        //                     ClockOutTime = null,
        //                     Breaks = new List<BreakPeriodViewModel>(),
        //                     TotalWorkedHours = TimeSpan.Zero,
        //                     TotalBreakHours = TimeSpan.Zero
        //                 });
        //             }
        //         }

        //         ViewBag.SelectedDate = date.Value;
        //         ViewBag.ViewType = viewType;
        //         ViewBag.SortOrder = sortOrder;
        //         return View(model);
        //     }

        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(int id, DateTime clockIn, DateTime? clockOut, bool isOnBreak)
        {
            var record = await _context.Attendance.FindAsync(id);
            if (record == null)
                return NotFound();

            record.ClockIn = clockIn;
            record.ClockOut = clockOut;
            // Can't assign to IsOnBreak if it's read-only (NotMapped calculated property)

            await _context.SaveChangesAsync();
            return Ok();
        }

        [HttpPost]

        public async Task<IActionResult> CreateManualEntry([FromBody] ManualEntryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new { success = false, message = "Validation failed", errors });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.Id == model.EmployeeId);
            if (employee == null)
            {
                return NotFound(new { success = false, message = "Employee not found" });
            }

            // Parse time strings
            if (!TimeSpan.TryParse(model.ClockInTime, out var clockInTime))
            {
                return BadRequest(new { success = false, message = "Invalid ClockInTime format. Use HH:mm (e.g., 09:00)." });
            }

            TimeSpan? clockOutTime = null;
            if (!string.IsNullOrEmpty(model.ClockOutTime))
            {
                if (!TimeSpan.TryParse(model.ClockOutTime, out var parsedClockOutTime))
                {
                    return BadRequest(new { success = false, message = "Invalid ClockOutTime format. Use HH:mm (e.g., 17:00)." });
                }
                clockOutTime = parsedClockOutTime;
            }

            // Time conversion
            TimeZoneInfo indianTimeZone;
            try
            {
                indianTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch
            {
                indianTimeZone = TimeZoneInfo.Utc;
            }

            var clockIn = new DateTime(model.Date.Year, model.Date.Month, model.Date.Day,
                                      clockInTime.Hours, clockInTime.Minutes, 0);
            clockIn = TimeZoneInfo.ConvertTimeToUtc(clockIn, indianTimeZone);

            DateTime? clockOut = null;
            if (clockOutTime.HasValue)
            {
                var clockOutLocal = new DateTime(model.Date.Year, model.Date.Month, model.Date.Day,
                                                clockOutTime.Value.Hours, clockOutTime.Value.Minutes, 0);
                clockOut = TimeZoneInfo.ConvertTimeToUtc(clockOutLocal, indianTimeZone);
            }

            var attendance = new Attendance
            {
                EmployeeId = model.EmployeeId,
                CompanyId = employee.CompanyId,
                UserId = employee.UserId,
                ClockIn = clockIn,
                ClockOut = clockOut,
                IsManualEntry = true,
                CreatedBy = employee.Id
            };

            try
            {
                _context.Attendance.Add(attendance);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, id = attendance.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }


        public IActionResult Export()
        {
            var attendanceList = _context.Attendance
                .Include(a => a.Employee)
                .Include(a => a.Company)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance");

                // Headers
                worksheet.Cell(1, 1).Value = "Employee Name";
                worksheet.Cell(1, 2).Value = "Company";
                worksheet.Cell(1, 3).Value = "Clock In";
                worksheet.Cell(1, 4).Value = "Clock Out";
                worksheet.Cell(1, 5).Value = "Break Start";
                worksheet.Cell(1, 6).Value = "Break End";
                worksheet.Cell(1, 7).Value = "Total Hours";
                worksheet.Cell(1, 8).Value = "Break Duration";

                int row = 2;
                foreach (var record in attendanceList)
                {
                    worksheet.Cell(row, 1).Value = record.Employee?.FullName ?? "N/A";
                    worksheet.Cell(row, 2).Value = record.Company?.CompanyName ?? "N/A";
                    worksheet.Cell(row, 3).Value = record.ClockIn.ToString("g");
                    worksheet.Cell(row, 4).Value = record.ClockOut?.ToString("g") ?? "N/A";
                    worksheet.Cell(row, 5).Value = record.BreakStart?.ToString("g") ?? "N/A";
                    worksheet.Cell(row, 6).Value = record.BreakEnd?.ToString("g") ?? "N/A";
                    worksheet.Cell(row, 7).Value = record.TotalHours?.ToString(@"hh\:mm") ?? "N/A";
                    worksheet.Cell(row, 8).Value = record.TotalBreakDuration.ToString(@"hh\:mm");
                    row++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        "AttendanceReport.xlsx");
                }
            }
        }



    }
}
