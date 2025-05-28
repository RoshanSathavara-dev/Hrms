namespace Hrms_system.Models
{
    public class AdminAttendanceDashboardViewModel
    {
        public int TotalEmployees { get; set; }
        public int ClockedInToday { get; set; }
        public int LateArrivalsToday { get; set; }
        public int OnBreakNow { get; set; }
        public List<RecentAttendanceViewModel>? RecentAttendance { get; set; }
    }
}
