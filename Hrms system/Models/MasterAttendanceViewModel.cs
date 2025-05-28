namespace Hrms_system.Models
{
    public class MasterAttendanceViewModel
    {
        public AdminAttendanceDashboardViewModel? Dashboard { get; set; }
        public IEnumerable<Attendance>? AttendanceRecords { get; set; }
        public string? ViewType { get; set; }
        public string? SelectedMonth { get; set; }
        public IEnumerable<Employee>? Employees { get; set; }
    }
}
