// CalendarController.cs
using Hrms_system.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Security.Claims;

public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public IActionResult GetHolidays()
    {
        // Get current user's company ID
        var currentUserCompanyId = GetCurrentUserCompanyId();

        // If company ID is 0 (not found), return empty list
        if (currentUserCompanyId == 0)
        {
            return Json(new List<object>());
        }

        var holidays = _context.Holidays
            .Where(h => h.CompanyId == currentUserCompanyId) // Filter by company
            .Select(h => new
            {
                id = h.Id,
                title = h.Title,
                start = h.Date.ToString("yyyy-MM-dd"),
                color = "#ff4d4d",
                display = "auto"
            }).ToList();

        return Json(holidays);
    }

    private int GetCurrentUserCompanyId()
    {
        // Implementation depends on your authentication system
        // Example 1: If user has CompanyId claim
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim != null && int.TryParse(companyIdClaim.Value, out int companyId))
        {
            return companyId;
        }

        // Example 2: If you have User entity with CompanyId
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId != null)
        {
            var user = _context.Users.Find(userId);
            return user?.CompanyId ?? 0;
        }

        return 0;
    }
}