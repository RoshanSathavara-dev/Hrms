namespace Hrms_system.Models
{
    public class BreakLog
    {
        public int Id { get; set; }
        public int AttendanceId { get; set; }
        public DateTime BreakStart { get; set; }
        public DateTime? BreakEnd { get; set; }

        public Attendance? Attendance { get; set; }
    }
}
