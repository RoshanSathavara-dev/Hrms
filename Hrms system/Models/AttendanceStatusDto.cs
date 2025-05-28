namespace Hrms_system.Models
{
    public class AttendanceStatusDto
    {
        public bool IsClockedIn { get; set; }
        public string? ClockInTime { get; set; }
        public bool IsOnBreak { get; set; }
        public string? BreakStartTime { get; set; } // Can be null
        public int PreviousWorkSeconds { get; set; }
    }
}
