namespace Hrms_system.Models
{
    public class PayrollRun
    {
        public int Id { get; set; }
        public string? Period { get; set; }
        public DateTime RunDate { get; set; } = DateTime.UtcNow;
        public int EmployeesCount { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalNet { get; set; }
        public string? Status { get; set; } // "Pending", "Completed", "Failed"
        public string? RunBy { get; set; }
    }
}
