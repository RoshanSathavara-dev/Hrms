namespace Hrms_system.Models
{
    public class AssignLeavesViewModel
    {
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int LeaveTypeId { get; set; }
        public string? LeaveTypeName { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal DefaultLeavesAllowed { get; set; }

        public List<Employee> Employees { get; set; } = new List<Employee>();
        public List<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();
        public List<LeaveAssignment> Assignments { get; set; } = new List<LeaveAssignment>();
    }
}
