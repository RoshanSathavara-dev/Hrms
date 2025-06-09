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
            var company = await _context.Companies
               .Include(c => c.Addresses) // Include addresses
               .Include(c => c.Designations)
                .Include(c => c.Departments)
               .FirstOrDefaultAsync(c => c.UserId == userId);

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


        // GET: Company/AddAddress
        public IActionResult AddAddress(int companyId)
        {
            ViewBag.CompanyId = companyId;
            return PartialView("_AddEditAddress", new Address { CompanyId = companyId });
        }

        // POST: Company/AddAddress
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAddress(Address address)
        {
            if (ModelState.IsValid)
            {
                // Check for existing Registered or Corporate office
                if (address.AddressType == "Registered" || address.AddressType == "Corporate")
                {
                    var existingAddress = await _context.Addresses
                        .AnyAsync(a => a.CompanyId == address.CompanyId && a.AddressType == address.AddressType);
                    if (existingAddress)
                    {
                        ModelState.AddModelError("AddressType", $"A {address.AddressType} office already exists.");
                        ViewBag.CompanyId = address.CompanyId;
                        return PartialView("_AddEditAddress", address);
                    }
                }

                _context.Add(address);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Address added successfully!";
                return Json(new { success = true });
            }
            ViewBag.CompanyId = address.CompanyId;
            return PartialView("_AddEditAddress", address);
        }

        // GET: Company/EditAddress/5
        public async Task<IActionResult> EditAddress(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address == null)
            {
                return NotFound();
            }
            ViewBag.CompanyId = address.CompanyId;
            return PartialView("_AddEditAddress", address);
        }

        // POST: Company/EditAddress/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAddress(int id, Address address)
        {
            if (id != address.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                // Fetch the existing address first
                var existingAddress = await _context.Addresses.FindAsync(id);

                if (existingAddress == null)
                {
                    return NotFound();
                }

                // Check for existing Registered or Corporate office (if type is being changed or is already one of these)
                if (address.AddressType == "Registered" || address.AddressType == "Corporate")
                {
                    var duplicateAddress = await _context.Addresses
                        .FirstOrDefaultAsync(a => a.CompanyId == address.CompanyId && a.AddressType == address.AddressType && a.Id != address.Id);
                    if (duplicateAddress != null)
                    {
                        ModelState.AddModelError("AddressType", $"A {address.AddressType} office already exists.");
                        ViewBag.CompanyId = address.CompanyId;
                        // Pass the existingAddress back to the partial view to retain current data if validation fails
                        return PartialView("_AddEditAddress", existingAddress);
                    }
                }

                // Update properties of the existing address with values from the incoming model
                existingAddress.AddressLine1 = address.AddressLine1;
                existingAddress.AddressLine2 = address.AddressLine2;
                existingAddress.City = address.City;
                existingAddress.State = address.State;
                existingAddress.Country = address.Country;
                existingAddress.Pincode = address.Pincode;
                // Note: We are not allowing changing AddressType here based on your partial view logic


                try
                {
                    _context.Update(existingAddress); // Mark the fetched entity as modified
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Address updated successfully!";
                    return Json(new { success = true });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AddressExists(address.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception) // Catch other potential exceptions
                {
                    // Log the exception (in a real application)
                    // _logger.LogError(ex, "Error updating address ID {AddressId}", id);
                    return StatusCode(500, new { success = false, message = "An unexpected error occurred while saving the address." });
                }
            }
            // If ModelState is not valid, return the partial view with errors
            ViewBag.CompanyId = address.CompanyId;
            // Pass the incoming address back to the partial view to display user input
            return PartialView("_AddEditAddress", address);
        }


        // POST: Company/DeleteAddress/5
        [HttpPost, ActionName("DeleteAddress")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAddressConfirmed(int id)
        {
            var address = await _context.Addresses.FindAsync(id);
            if (address != null)
            {
                _context.Addresses.Remove(address);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Address deleted successfully!"; // You can keep this if you want the TempData message on reload
                // *** CHANGE THIS LINE ***
                return Json(new { success = true, message = "Address deleted successfully!" }); // Return JSON on success
            }

            // Handle case where address is not found
            return Json(new { success = false, message = "Address not found." });
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddDesignation([FromBody] Designation designation)
        {
            // Validate incoming designation data
            if (designation == null || string.IsNullOrWhiteSpace(designation.Title))
            {
                return Json(new { success = false, message = "Designation title cannot be empty." });
            }

            var userId = _userManager.GetUserId(User);
            // Include Designations when fetching company to potentially use it later or for consistency
            var company = await _context.Companies
                               .Include(c => c.Designations) // Included Designations
                               .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return Json(new { success = false, message = "Company profile not found." });
            }

            // Check if a designation with the same title already exists for this company (case-insensitive)
            // Safely handle d.Title possibly being null in the comparison
            var existingDesignation = await _context.Designations
                                                .AnyAsync(d => d.CompanyId == company.Id && (d.Title != null && d.Title.ToLower() == designation.Title.ToLower()));


            if (existingDesignation)
            {
                return Json(new { success = false, message = "Designation with this title already exists." });
            }

            // Associate the new designation with the company
            designation.CompanyId = company.Id;

            _context.Designations.Add(designation);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Designation added successfully!" });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SaveDepartments([FromBody] List<Department> departments)
        {
            if (departments == null)
            {
                return Json(new { success = false, message = "No department data received." });
            }

            var userId = _userManager.GetUserId(User);
            var company = await _context.Companies
                                .Include(c => c.Departments)
                                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (company == null)
            {
                return Json(new { success = false, message = "Company profile not found." });
            }

            // Dictionary to keep track of departments received from the frontend by their (temporary) ID or name
            var receivedDepartmentsMap = departments.ToDictionary(d => d.Id != 0 ? d.Id : -departments.IndexOf(d) - 1); // Use negative temporary IDs for new departments

            // Process existing departments
            foreach (var existingDepartment in company.Departments?.ToList() ?? new List<Department>())// ToList() to avoid modifying collection while iterating
            {
                if (receivedDepartmentsMap.TryGetValue(existingDepartment.Id, out var updatedDepartment))
                {
                    // Update existing department
                    existingDepartment.Name = updatedDepartment.Name;
                    existingDepartment.SubDepartments = updatedDepartment.SubDepartments;
                    existingDepartment.DepartmentHeadId = updatedDepartment.DepartmentHeadId; // Needs refinement for Employee relationship
                    _context.Entry(existingDepartment).State = EntityState.Modified;
                    receivedDepartmentsMap.Remove(existingDepartment.Id); // Remove updated department from map
                }
                else
                {
                    // Department was removed on the frontend - mark for deletion
                    _context.Departments.Remove(existingDepartment);
                }
            }

            // Add new departments
            foreach (var newDepartment in receivedDepartmentsMap.Values)
            {
                // Only add if it's a new department (temporary negative ID)
                if (newDepartment.Id <= 0) // Check if it's a temporary ID
                {
                    newDepartment.CompanyId = company.Id; // Associate with the company
                    newDepartment.Id = 0; // Ensure Id is 0 for new entities
                    _context.Departments.Add(newDepartment);
                }
                // Note: Departments with positive IDs that are still in receivedDepartmentsMap were not found in the existing list
                // This scenario might indicate an issue or a department added by another user/process.
                // For now, we assume the frontend sends all current departments.
                // A more robust approach might involve checking for conflicts.
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Departments updated successfully!";
                return Json(new { success = true, message = "Departments saved successfully!" });
            }
            catch (Exception ex)
            {
                // Log the exception (in a real application)
                // _logger.LogError(ex, "Error saving departments for company {CompanyId}", company.Id);
                return Json(new { success = false, message = "Error saving departments: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return Json(new { success = false, message = "Department not found." });
            }

            try
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();
                // Optionally remove from Company's collection if it was loaded
                // company.Departments.Remove(department);

                return Json(new { success = true, message = "Department deleted successfully." });
            }
            catch (Exception ex)
            {
                // Log the exception (in a real application)
                // _logger.LogError(ex, "Error deleting department ID {DepartmentId}", id);
                return Json(new { success = false, message = "Error deleting department: " + ex.Message });
            }
        }

        private bool AddressExists(int id)
        {
            return _context.Addresses.Any(e => e.Id == id);
        }


    }
}