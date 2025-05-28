namespace Hrms_system.Models
{
    public class AttendanceComplianceViewModel
    {
        public int EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime Date { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public TimeSpan BreakDuration { get; set; }
        public TimeSpan ExpectedHours { get; set; }
        public TimeSpan ActualHours { get; set; }
        public string? Status { get; set; }
    }
}
