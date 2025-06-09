using System;
using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class Announcement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string? Title { get; set; }

        [Required]
        public string? Content { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [Required]
        public bool IsActive { get; set; }

        public string? CreatedBy { get; set; }

        public string? Department { get; set; }  // If announcement is department specific

        [Required]
        public int CompanyId { get; set; }

        public Company? Company { get; set; }
    }
}