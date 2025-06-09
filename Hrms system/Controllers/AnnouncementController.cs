using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hrms_system.Models;
using Hrms_system.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;


namespace Hrms_system.Controllers
{
    [Authorize]
    public class AnnouncementController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AnnouncementController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Announcement
        // GET: Announcement
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge(); // Or redirect to login
            }

            IQueryable<Announcement> announcements = _context.Announcements.Include(a => a.Company);

            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                // Admins/HR can see all announcements within their company
                if (currentUser.CompanyId.HasValue)
                {
                    announcements = announcements.Where(a => a.CompanyId == currentUser.CompanyId.Value);
                }
                else
                {
                    // Optionally handle case where Admin/HR has no CompanyId - maybe they see nothing or all?
                    // For now, let's assume they must have a CompanyId to manage company-specific announcements
                    return View(new List<Announcement>()); // No company associated, no announcements to show
                }
                ViewData["Layout"] = "_AdminLayout";
            }
            else
            {
                // Employees only see active announcements for their company or all departments
                if (currentUser.CompanyId.HasValue)
                {
                    announcements = announcements.Where(a =>
                        a.IsActive &&
                        a.CompanyId == currentUser.CompanyId.Value &&
                        (string.IsNullOrEmpty(a.Department) || a.Department == currentUser.Department)
                    );
                }
                else
                {
                    // Employees without CompanyId see nothing
                    return View(new List<Announcement>());
                }
                ViewData["Layout"] = "_Layout";
            }

            return View(await announcements.OrderByDescending(a => a.CreatedDate).ToListAsync());
        }


        // GET: Announcement/Create
        [Authorize(Roles = "Admin,HR")]
        public IActionResult Create()
        {
            ViewData["Layout"] = "_AdminLayout";
            return View();
        }

        // POST: Announcement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Create([Bind("Title,Content,ExpiryDate,Department")] Announcement announcement)
        {
            if (!ModelState.IsValid)
            {
                foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // Log the error or add it to TempData for display
                    TempData["Error"] = modelError.ErrorMessage;
                }
                ViewData["Layout"] = "_AdminLayout";
                return View(announcement);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.CompanyId == null)
                {
                    TempData["Error"] = "User is not associated with any company.";
                    ViewData["Layout"] = "_AdminLayout";
                    return View(announcement);
                }

                announcement.CreatedDate = DateTime.Now;
                announcement.IsActive = true;
                announcement.CreatedBy = user.UserName;
                announcement.CompanyId = user.CompanyId.Value;

                _context.Add(announcement);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Announcement created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error creating announcement: {ex.Message}";
                ViewData["Layout"] = "_AdminLayout";
                return View(announcement);
            }
        }


        // GET: Announcement/Edit/5
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == user.CompanyId);

            if (announcement == null)
            {
                return NotFound();
            }

            ViewData["Layout"] = "_AdminLayout";
            return View(announcement);
        }

        // POST: Announcement/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Content,ExpiryDate,Department,IsActive")] Announcement announcement)
        {
            if (id != announcement.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var user = await _userManager.GetUserAsync(User);
                    var existingAnnouncement = await _context.Announcements
                        .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == user.CompanyId);

                    if (existingAnnouncement == null)
                    {
                        return NotFound();
                    }

                    existingAnnouncement.Title = announcement.Title;
                    existingAnnouncement.Content = announcement.Content;
                    existingAnnouncement.ExpiryDate = announcement.ExpiryDate;
                    existingAnnouncement.Department = announcement.Department;
                    existingAnnouncement.IsActive = announcement.IsActive;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["Layout"] = "_AdminLayout";
            return View(announcement);
        }

        // POST: Announcement/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            var announcement = await _context.Announcements
                .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == user.CompanyId);

            if (announcement != null)
            {
                announcement.IsActive = false;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.Id == id);
        }
    }
}