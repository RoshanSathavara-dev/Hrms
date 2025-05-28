namespace Hrms_system.Models
{
    public class PayrollViewModel
    {
        public int EmployeeId { get; set; }
        public string? EmployeeNumber { get; set; }
        public string?  FullName { get; set; }
        public string? Department { get; set; }
        public string? Position { get; set; }
        public decimal Salary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPay => (Salary + Allowances) - Deductions;
        public string? Status { get; set; }
    }
}
