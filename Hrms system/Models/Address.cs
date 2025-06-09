using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class Address
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company? Company { get; set; } // Navigation property

        [Required]
        public string? AddressType { get; set; } // e.g., "Registered", "Corporate", "Branch"

        [Required]
        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        [Required]
        public string? City { get; set; }

        [Required]
        public string? State { get; set; }

        [Required]
        public string? Country { get; set; }

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be 6 digits.")]
        public string? Pincode { get; set; }
    }
}
