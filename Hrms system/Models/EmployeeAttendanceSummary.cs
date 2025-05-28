namespace Hrms_system.Models
{
    public class EmployeeAttendanceSummary
    {
        public int TotalDays { get; set; }
        public TimeSpan TotalHours { get; set; }
        public TimeSpan AverageHoursPerDay { get; set; }
        public int LateArrivals { get; set; }
        public int EarlyDepartures { get; set; }
        public int MissedDays { get; set; }
    }
}
