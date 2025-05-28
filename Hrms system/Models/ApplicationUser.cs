    using Microsoft.AspNetCore.Identity;

namespace Hrms_system.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; } = string.Empty;
        public string JobTitle { get; set; } = "Software Developer";
        public string ProfileImageUrl { get; set; } = "/images/default-profile.png";
        public string PersonalEmail { get; set; } = string.Empty;
        public string AlternatePhone { get; set; } = "-"; // Default value added
        public string BloodGroup { get; set; } = "-";
        public DateTime? DateOfBirth { get; set; }
        public string Gender { get; set; } = "Not Specified";
        public string CurrentAddress { get; set; } = "-";
        public string PermanentAddress { get; set; } = "-";

        public string Department { get; set; } = "-";
        public string Position { get; set; } = "Software Developer"; // Renamed from JobTitle

        public DateTime JoiningDate { get; set; }

        public int? CompanyId { get; set; } // Nullable if needed
        public Company? Company { get; set; } // Navigation property

    }
}
