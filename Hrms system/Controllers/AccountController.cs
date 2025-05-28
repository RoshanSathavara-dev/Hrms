    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Identity;
    using System.Threading.Tasks;
    using Hrms_system.ViewModels;
    using Microsoft.Extensions.Logging;
    using Microsoft.AspNetCore.Authentication;
    using Hrms_system.Models;
    using Microsoft.EntityFrameworkCore;
    using Hrms_system.Data;

    namespace Hrms_system.Controllers
    {
        public class AccountController : Controller
        {
            private readonly UserManager<ApplicationUser> _userManager;
            private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly ILogger<AccountController> _logger;
            private readonly ApplicationDbContext _context;

            public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger, ApplicationDbContext context)
            {
                _userManager = userManager;
                _signInManager = signInManager;
                _logger = logger;
                _context = context;
            }

            // 🔹 Register GET
            [HttpGet]
            public IActionResult Register() => View();

    
            [HttpPost]
            public async Task<IActionResult> Register(RegisterViewModel model)
            {
                if (!ModelState.IsValid)
                    return View(model);

                // 1. Create user
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // 2. Create and save company, now that user is created and has Id
                var company = new Company
                {
                    CompanyName = model.CompanyName,
                    Address = model.Address,
                    GSTNumber = model.GSTNumber,
                    ContactEmail = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    CreatedDate = DateTime.Now,
                    UserId = user.Id
                };

                _context.Companies.Add(company);
                await _context.SaveChangesAsync();

                // 3. Update user's CompanyId now that company is created
                user.CompanyId = company.Id;
                await _userManager.UpdateAsync(user);
            HttpContext.Session.SetInt32("CompanyId", company.Id);



            // 4. Sign in and redirect
            await _userManager.AddToRoleAsync(user, "Admin");
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Admin");
            }





            // ✅ Login
            [HttpGet]
            public async Task<IActionResult> Login(string? email = null)
            {
                if (!string.IsNullOrEmpty(email))
                {
                    var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == email);
                    if (employee != null && employee.IsFirstLogin)
                    {
                        return View(new LoginViewModel
                        {
                            Email = email,
                            IsFirstLogin = true,
                            ShowPasswordSetup = true
                        });
                    }
                }

                // FIX: Pass an empty model to avoid null reference
                return View(new LoginViewModel());
            }


            [HttpPost]
            public async Task<IActionResult> Login(LoginViewModel model)
            {
                // Always get the employee record first
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Email == model.Email);

                // Set these flags based on employee status
                model.IsFirstLogin = employee?.IsFirstLogin ?? false;
                model.ShowPasswordSetup = model.IsFirstLogin;

                if (model.IsFirstLogin)
                {
                    if (!model.IsTemporaryPasswordVerified)
                    {
                        // First step: Verify temporary password entered in Password field
                        if (string.IsNullOrEmpty(model.Password))
                        {
                            ModelState.AddModelError("Password", "Please enter your temporary password");
                            return View(model);
                        }

                        if (employee!.TemporaryPassword != model.Password)
                        {
                            ModelState.AddModelError("Password", "Invalid temporary password");
                            return View(model);
                        }

                        // If we get here, temporary password was correct
                        model.IsTemporaryPasswordVerified = true;
                        model.Password = string.Empty; // Clear the password field
                    ModelState.Clear(); // Clear previous validation
                        return View(model);
                    }
                    else
                    {
                        // Second step: Set new password
                        if (string.IsNullOrEmpty(model.NewPassword))
                        {
                            ModelState.AddModelError("NewPassword", "Please enter a new password");
                            return View(model);
                        }

                        if (model.NewPassword != model.ConfirmPassword)
                        {
                            ModelState.AddModelError("ConfirmPassword", "Passwords don't match");
                            return View(model);
                        }

                        return await HandleFirstTimeLogin(model, employee!);
                    }
                }

                // Regular login flow
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);

                    if (user == null)
                    {
                        // Optional: Log this or redirect to an error page
                        ModelState.AddModelError("", "User not found.");
                        return View(model);
                    }

                HttpContext.Session.SetString("UserId", user.Id);
                HttpContext.Session.SetInt32("CompanyId", user.CompanyId ?? 0);

                return await RedirectToLocal(user); // Now guaranteed non-null
                }


                ModelState.AddModelError(string.Empty, "Invalid login attempt");
                return View(model);
            }








            // ✅ Logout
            [HttpPost]
            public async Task<IActionResult> Logout()
            {
                // Clear the existing external cookie
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                _logger.LogInformation("✅ User has been logged out.");
                return RedirectToAction("Login");
            }

            private async Task<IActionResult> HandleFirstTimeLogin(LoginViewModel model, Employee employee)
            {
                // Find or create the user
                var user = await _userManager.FindByEmailAsync(model.Email) ?? new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    CompanyId = employee.CompanyId
                };

                // Set or reset the password
                if (await _userManager.HasPasswordAsync(user))
                {
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword ?? throw new ArgumentNullException(nameof(model.NewPassword)));
                    if (!resetResult.Succeeded)
                    {
                        AddErrors(resetResult);
                        return View("Login", model);
                    }
                }
                else
                {
                    var createResult = await _userManager.CreateAsync(user, model.NewPassword ?? throw new ArgumentNullException(nameof(model.NewPassword)));
                    if (!createResult.Succeeded)
                    {
                        AddErrors(createResult);
                        return View("Login", model);
                    }
                }

                // Ensure user has Employee role
                if (!await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    await _userManager.AddToRoleAsync(user, "Employee");
                }

                // Update employee record
                employee.IsFirstLogin = false;
                employee.TemporaryPassword = null;
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                // Sign in and redirect
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home"); // Direct redirect instead of using RedirectToLocal
            }


            private void AddErrors(IdentityResult result)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            private async Task<IActionResult> RedirectToLocal(ApplicationUser user)
            {
                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (await _userManager.IsInRoleAsync(user, "Employee"))
                {
                    return RedirectToAction("Index", "Home"); // Ensure this matches your route
                }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SilentLogout()
        {
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            _logger.LogInformation("User logged out via silent logout (browser close).");
            return Ok();
        }



    }
    }

