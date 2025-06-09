using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class WorkWeekRule
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? WeeklyPatternJson { get; set; }

        public bool IsDefault { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }
    }
}