using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class Designation
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public Company? Company { get; set; }

        [Required]
        [StringLength(100)]
        public string? Title { get; set; }

        // Consider adding a count of employees later if needed
        // public int EmployeeCount { get; set; }
    }
}
