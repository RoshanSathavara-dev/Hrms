namespace Hrms_system.Models
{
    public class LeaveBalanceViewModel
    {
        public string? EmployeeName { get; set; }
        public string? LeaveTypeName { get; set; }
        public decimal TotalLeaves { get; set; }
        public decimal UsedLeaves { get; set; }
        public decimal PendingLeaves { get; set; }
        public decimal AvailableLeaves { get; set; }
        public decimal? CarryForwardedLeaves { get; set; }
        public DateTime? CarryForwardExpiry { get; set; }
        public int Year { get; set; }
    }
}