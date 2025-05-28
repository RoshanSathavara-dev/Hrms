namespace Hrms_system.Models
{
    public class DailyAttendanceViewModel
    {
        public DateTime Date { get; set; }
        public TimeSpan ClockInTime { get; set; }
        public TimeSpan? ClockOutTime { get; set; }
        public List<BreakPeriodViewModel> Breaks { get; set; } = new List<BreakPeriodViewModel>();
        public TimeSpan TotalWorkedHours { get; set; }
        public TimeSpan TotalBreakHours { get; set; }
        public TimeSpan RemainingTime { get; set; }
    }

    public class BreakPeriodViewModel
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
    }
}
