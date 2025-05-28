using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hrms_system.Models;
using Hrms_system.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Hrms_system.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CompanyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CompanyController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Company/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                company = new Company { UserId = userId };
            }


            // Get total employee count for the company
            var totalEmployees = await _context.Employees
                .CountAsync(e => e.CompanyId == company.Id);

            ViewBag.TotalEmployees = totalEmployees;


            return View(company);
        }


        // POST: Company/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Company company)
        {
            if (ModelState.IsValid)
            {
                if (company.Id == 0)
                {
                    // Add new company
                    company.CreatedDate = DateTime.Now;
                    _context.Add(company);
                }
                else
                {
                    // Update existing company
                    _context.Update(company);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Company profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }
            return View(company);
        }


        [HttpPost]
        public async Task<IActionResult> UploadLogo(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return Json(new { success = false, message = "No file selected" });
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
            {
                return Json(new { success = false, message = "Invalid file type. Only image files are allowed." });
            }

            if (file.Length > 2 * 1024 * 1024)
            {
                return Json(new { success = false, message = "File size exceeds the 2MB limit." });
            }

            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
            if (company == null)
            {
                return Json(new { success = false, message = "Company profile not found." });
            }

            try
            {
                var uniqueFileName = $"logo_{Guid.NewGuid()}{extension}";
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");

                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                var savePath = Path.Combine(folderPath, uniqueFileName);

                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Save path in DB
                company.LogoPath = $"/uploads/profile/{uniqueFileName}";
                _context.Update(company);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Logo uploaded successfully", imageUrl = company.LogoPath });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error uploading file: " + ex.Message });
            }
        }

    }
}