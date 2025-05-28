using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class Company
    {
        public int Id { get; set; }

        public string? UserId { get; set; }


        [Required]
        public string CompanyName { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string? GSTNumber { get; set; }
        public string? ContactEmail { get; set; }
        public string? PhoneNumber { get; set; }

        public string? LogoPath { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

  
        public ICollection<Employee>? Employees { get; set; } // Navigation property
    }
}
