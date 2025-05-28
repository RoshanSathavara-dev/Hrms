namespace Hrms_system.Models
{
    public class UserProfileViewModel
    {
        public string? Name { get; set; }
        public string? JobTitle { get; set; }
        public string? ProfileImageUrl { get; set; }
        public string? OfficialEmail { get; set; }
        public string? PersonalEmail { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AlternatePhone { get; set; }
        public string? BloodGroup { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? CurrentAddress { get; set; }
        public string? PermanentAddress { get; set; }

        public string? Department { get; set; }
    }
}
