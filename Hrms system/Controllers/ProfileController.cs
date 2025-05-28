using Microsoft.AspNetCore.Identity;
using Hrms_system.Data;
using Microsoft.AspNetCore.Mvc;
using Hrms_system.Models;


namespace Hrms_system.Controllers
{
    public class ProfileController : Controller
    {

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProfileController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userProfile = new UserProfileViewModel
            {
                Name = user.FullName,
                JobTitle = user.JobTitle ?? "Software Developer",
                ProfileImageUrl = user.ProfileImageUrl ?? "/images/default-profile.png",
                OfficialEmail = user.Email,
                PersonalEmail = user.PersonalEmail,
                PhoneNumber = user.AlternatePhone,
                AlternatePhone = user.AlternatePhone,
                BloodGroup = user.BloodGroup ?? "-",
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                CurrentAddress = user.CurrentAddress ?? "-",
                PermanentAddress = user.PermanentAddress ?? "-"
            };

            return View(userProfile);
        }


        //[HttpPost]
        //public async Task<IActionResult> Edit(UserProfileViewModel model)
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null) return RedirectToAction("Login", "Account");

        //    // ✅ Name and Official Email cannot be edited
        //    user.PersonalEmail = model.PersonalEmail ?? string.Empty;
        //    user.PhoneNumber = model.PhoneNumber ?? string.Empty;
        //    user.AlternatePhone = model.AlternatePhone ?? "-";
        //    user.BloodGroup = model.BloodGroup ?? "-";
        //    user.Gender = model.Gender ?? "Not Specified";
        //    user.CurrentAddress = model.CurrentAddress ?? "-";
        //    user.PermanentAddress = model.PermanentAddress ?? "-";
        //    user.DateOfBirth = model.DateOfBirth;


        //    var result = await _userManager.UpdateAsync(user);
        //    if (result.Succeeded)
        //    {
        //        TempData["Success"] = "Profile updated successfully!";
        //        return RedirectToAction("Index");
        //    }

        //    TempData["Error"] = "Failed to update profile.";
        //    return View("Index", model);
        //}

        [HttpPost]
        public async Task<IActionResult> UpdateContact(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.PersonalEmail = model.PersonalEmail ?? string.Empty;
            user.PhoneNumber = model.PhoneNumber ?? string.Empty;
            user.AlternatePhone = model.AlternatePhone ?? "-";

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Contact information updated successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to update contact information.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePersonal(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.BloodGroup = model.BloodGroup ?? "-";
            user.Gender = model.Gender ?? "Not Specified";
            user.DateOfBirth = model.DateOfBirth;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Personal information updated successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to update personal information.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAddress(UserProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.CurrentAddress = model.CurrentAddress ?? "-";
            user.PermanentAddress = model.PermanentAddress ?? "-";

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Address updated successfully!";
                return RedirectToAction("Index");
            }

            TempData["Error"] = "Failed to update address.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UploadProfileImage(IFormFile profileImage)
        {
            if (profileImage != null && profileImage.Length > 0)
            {
                var userId = HttpContext.Session.GetInt32("UserId"); // or however you're storing current user
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profile");
                Directory.CreateDirectory(uploadsFolder);

                var fileName = $"{userId}_{Path.GetFileName(profileImage.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profileImage.CopyToAsync(stream);
                }

                // Save path to database (e.g., /uploads/profile/filename.jpg)
                var imageUrl = $"/uploads/profile/{fileName}";
                // Update user profile
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.ProfileImageUrl = imageUrl;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, imageUrl = imageUrl });
            }

            return Json(new { success = false });
        }



    }
}
