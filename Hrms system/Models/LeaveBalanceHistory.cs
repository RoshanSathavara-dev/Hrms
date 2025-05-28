using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class LeaveBalanceHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EmployeeLeaveBalanceId { get; set; }

        [Precision(10, 2)]
        public decimal PreviousTotal { get; set; }

        [Precision(10, 2)]
        public decimal NewTotal { get; set; }

        [Required]
        [StringLength(500)]
        public string? ChangeReason { get; set; }

        public DateTime ChangedOn { get; set; } = DateTime.UtcNow;

        [ForeignKey("EmployeeLeaveBalanceId")]
        public virtual EmployeeLeaveBalance? EmployeeLeaveBalance { get; set; }
    }
}
