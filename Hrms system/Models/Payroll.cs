namespace Hrms_system.Models
{
    public class Payroll
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public string? Period { get; set; } // e.g. "June 2023"
        public DateTime PaymentDate { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPay { get; set; }
        public string? Status { get; set; } // "Draft", "Processed", "Paid"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
