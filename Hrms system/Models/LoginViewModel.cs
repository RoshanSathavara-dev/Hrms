using System.ComponentModel.DataAnnotations;

namespace Hrms_system.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Temporary Password")]
        public string? TemporaryPassword { get; set; }

        public bool IsTemporaryPasswordVerified { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords don't match.")]
        public string? ConfirmPassword { get; set; }

        public bool RememberMe { get; set; }
        public bool IsFirstLogin { get; set; } = false;
        public bool ShowPasswordSetup { get; set; } = false;

        // Add this new property
        public string? ReturnUrl { get; set; }
    }
}
