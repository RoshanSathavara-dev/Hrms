using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }  // Link attendance to a user

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }   

        [Required]
        public DateTime ClockIn { get; set; } // When the user clocked in

        public DateTime? ClockOut { get; set; } // When the user clocked out

        public DateTime? BreakStart { get; set; } // ✅ Store when a break starts

        public DateTime? BreakEnd { get; set; } // ✅ Store when a break ends

        [NotMapped] // Not stored in DB, calculated field
        public TimeSpan? TotalHours => ClockOut.HasValue ? ClockOut.Value - ClockIn : null;

        public TimeSpan TotalBreakDuration { get; set; } = TimeSpan.Zero;

        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        public int? CompanyId { get; set; }

        [ForeignKey("CompanyId")]
        public virtual Company? Company { get; set; }

        public bool IsManualEntry { get; set; } = false;


        public int CreatedBy { get; set; }

        // Add this computed property
        [NotMapped]
        public bool IsOnBreak => BreakLogs.Any(b => b.BreakEnd == null);

        public int BreakCount { get; set; } = 0;

        public int BreakViolations { get; set; }

        public ICollection<BreakLog> BreakLogs { get; set; } = new List<BreakLog>();



    }
}
