using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        // Foreign key for Company
        public int CompanyId { get; set; }

        // Navigation property to the parent Company
        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        // Placeholder properties based on the image - may need refinement later
        public string? SubDepartments { get; set; } // Example: could be a comma-separated string or a separate relationship

        // Department Head - will likely be a relationship to an Employee model
        // For now, using a simple property, but this needs proper implementation later
        public int? DepartmentHeadId { get; set; }
        // public Employee DepartmentHead { get; set; } // Uncomment and implement when Employee model is ready
    }
}
