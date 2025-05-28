namespace Hrms_system.Models
{
    public class RecentAttendanceViewModel
    {
        public string? EmployeeName { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public string? Status { get; set; }
    }
}
