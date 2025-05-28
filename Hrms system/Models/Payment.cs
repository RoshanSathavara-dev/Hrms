using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        public string PaymentMethod { get; set; } = "Bank Transfer"; // Bank Transfer, Cash, Check, etc.

        public string? TransactionReference { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

        public string? Notes { get; set; }

        public int? PayrollId { get; set; }
        public Payroll? Payroll { get; set; }
    }
}