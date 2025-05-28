namespace Hrms_system.Models
{
    public class LeaveDashboardViewModel
    {
        public List<LeaveRequest>? LeaveRequests { get; set; } = new();
        public List<EmployeeLeaveBalance>? LeaveBalances { get; set; } = new();
    }
}
