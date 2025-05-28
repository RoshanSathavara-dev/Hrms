using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? UserId { get; set; }  // Foreign key to IdentityUser

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }



        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime AppliedOn { get; set; }     

        [Required]
        public string? Reason { get; set; }

        public string Status { get; set; } = "Pending"; // Default status

        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;


        //public int Days => (EndDate - StartDate).Days + 1;

        public decimal Days { get; set; }
        public int? CompanyId { get; set; } // Add this property
        public Company? Company { get; set; }

        public int LeaveTypeId { get; set; }
        public virtual LeaveType? LeaveType { get; set; }

        //public bool IsHalfDay { get; set; }
        //public bool IsFirstHalf { get; set; }

        public string? StartHalf { get; set; } // "First" or "Second"
        public string? EndHalf { get; set; } // "First" or "Second"

        public DateTime? ApprovedOn { get; set; }  // Add this new property
        public DateTime? RejectedOn { get; set; }  // Add this for rejection tracking
        public string? RejectionReason { get; set; }
    }
}
