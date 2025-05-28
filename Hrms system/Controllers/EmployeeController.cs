using Hrms_system.Data; // Your DbContext namespace
using Hrms_system.Models; // Your Employee model namespace
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Hrms_system.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<EmployeeController> _logger;
        private readonly IEmailService _emailService;


        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ILogger<EmployeeController> logger , IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var currentUserId = _userManager.GetUserId(User);
                if (string.IsNullOrEmpty(currentUserId))
                {
                    return Unauthorized(new { success = false, message = "User not logged in." });
                }

                var company = await _context.Companies
                    .FirstOrDefaultAsync(c => c.UserId == currentUserId);

                if (company == null)
                {
                    return BadRequest(new { success = false, message = "Company not found for current user." });
                }

                var employees = await _context.Employees
                    .Where(e => e.CompanyId == company.Id)
                    .Include(e => e.User) // Optional, might cause circular reference
                    .ToListAsync();

                var employeeList = employees.Select(e => new {
                    e.Id,
                    e.FirstName,
                    e.LastName,
                    e.Email,
                    e.Phone,
                    e.Department,
                    e.Position,
                    JoinDate = e.JoinDate.ToString("yyyy-MM-dd"),
                    e.Status
                }).ToList();

                return Json(new { success = true, data = employeeList });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAll()");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching employees.",
                    error = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }


        [HttpPost]
        public async Task<IActionResult> Add([FromBody] Employee model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors?.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                _logger.LogWarning("Invalid employee data submitted: {@Errors}", errors);
                return BadRequest(new { success = false, message = "Validation failed", errors = errors });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Get company info
                var currentUserId = _userManager.GetUserId(User);
                var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == currentUserId);

                if (company == null)
                {
                    _logger.LogWarning("No company found for logged-in user: {UserId}", currentUserId);
                    return BadRequest(new { success = false, message = "Company not found for this user." });
                }

                // Generate temp password
                var tempPassword = GenerateTemporaryPassword();

                // Create user first
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    CompanyId = company.Id
                };

                var createResult = await _userManager.CreateAsync(user, tempPassword);
                if (!createResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("Failed to create user for employee: {Email}", model.Email);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to create user account",
                        errors = createResult.Errors.Select(e => e.Description)
                    });
                }

                // Now assign the UserId to the employee model
                model.UserId = user.Id; // THIS IS THE CRITICAL LINE YOU'RE MISSING
                model.CompanyId = company.Id;
                model.IsFirstLogin = true;
                model.TemporaryPassword = tempPassword;
                model.JoinDate = DateTime.Now;

                // Save Employee to DB
                _context.Employees.Add(model);
                // ✅ Send email with login credentials
                var emailMessage = $@"
Dear {model.FirstName},

Your employee account has been created successfully.

Temporary Login Credentials:
----------------------------
Email: {model.Email}
Temporary Password: {tempPassword}

Please login and update your password as soon as possible.

Regards,
HR Team";
                if (string.IsNullOrWhiteSpace(model.Email))
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = "Email address is required." });
                }

                try
                {
                    await _emailService.SendTemplateEmailAsync(model.Email, new
                    {
                        FirstName = model.FirstName,
                        Email = model.Email,
                        Password = tempPassword
                    });
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Failed to send email for employee: {Email}", model.Email);
                    // Optionally, rollback the transaction if the email fails
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "Failed to send email" });
                }


                await _context.SaveChangesAsync();

                // Assign role
                var roleResult = await _userManager.AddToRoleAsync(user, "Employee");
                if (!roleResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError("Failed to assign Employee role to user: {Email}", model.Email);
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to assign employee role",
                        errors = roleResult.Errors.Select(e => e.Description)
                    });
                }

                await transaction.CommitAsync();
                _logger.LogInformation("Successfully created employee and user: {Email}", model.Email);

                return Ok(new
                {
                    success = true,
                    message = "Employee added successfully",
                    employeeId = model.Id,
                    userId = user.Id // Return the user ID for reference
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Exception while adding employee");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while adding employee",
                    error = ex.Message
                });
            }
        }






        private string GenerateTemporaryPassword()
        {
            // Generate a random 8-character password
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        //private async Task SendWelcomeEmail(string email, string tempPassword, string loginUrl)
        //{
        //    // Implement your email sending logic here
        //    // This is just a placeholder implementation
        //    try
        //    {
        //        var subject = "Welcome to Our HR System";
        //        var message = $@"
        //    <h1>Welcome!</h1>
        //    <p>Your account has been created. Please use the following temporary password to login:</p>
        //    <p><strong>Temporary Password:</strong> {tempPassword}</p>
        //    <p>Click <a href='{loginUrl}'>here</a> to login and set your permanent password.</p>
        //    <p>If the link doesn't work, copy and paste this URL into your browser: {loginUrl}</p>";

        //        // Use your email service (SendGrid, SMTP, etc.) here
        //        // await _emailSender.SendEmailAsync(email, subject, message);

        //        _logger.LogInformation($"Email sent to {email} with temp password");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error sending welcome email");
        //    }
        //}
        [HttpGet]
        public IActionResult GetById(int id)
        {
            var emp = _context.Employees.FirstOrDefault(e => e.Id == id);
            if (emp == null) return NotFound();
            return Json(emp);
        }

        [HttpPost]
        public IActionResult Update(Employee model)
        {
            var existing = _context.Employees.Find(model.Id);
            if (existing == null) return NotFound();

            existing.FirstName = model.FirstName;
            existing.LastName = model.LastName;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.Department = model.Department;
            existing.Position = model.Position;
            existing.JoinDate = model.JoinDate;
            existing.Status = model.Status;

            _context.SaveChanges();
            return Json(new { success = true, message = "Employee updated successfully." });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(employee.UserId);
            if (user == null)
            {
                // If somehow the user is already deleted, remove the employee record only
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Employee deleted. User not found." });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // First remove the employee record
                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                // Then delete the associated Identity user
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to delete associated user account.",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }

                await transaction.CommitAsync();
                return Json(new { success = true, message = "Employee and user deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while deleting the employee.",
                    error = ex.Message
                });
            }
        }


        public IActionResult GetEmployeeStats()
        {
            var currentUserId = _userManager.GetUserId(User);
            var company = _context.Companies.FirstOrDefault(c => c.UserId == currentUserId);
            if (company == null)
                return BadRequest(new { message = "Company not found." });

            var totalEmployees = _context.Employees.Count(e => e.CompanyId == company.Id);
            return Json(new { totalEmployees });
        }


    }
}
