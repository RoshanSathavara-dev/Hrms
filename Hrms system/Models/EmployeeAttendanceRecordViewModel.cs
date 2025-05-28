namespace Hrms_system.Models
{
    public class EmployeeAttendanceRecordViewModel
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public DateTime ClockIn { get; set; }
        public DateTime? ClockOut { get; set; }
        public TimeSpan BreakDuration { get; set; }
        public TimeSpan ExpectedHours { get; set; }
        public TimeSpan ActualHours { get; set; }
        public string? Status { get; set; }
        public bool IsLate { get; set; }
        public bool IsEarlyDeparture { get; set; }
    }
}
