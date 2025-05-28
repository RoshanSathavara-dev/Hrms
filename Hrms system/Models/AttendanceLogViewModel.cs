namespace Hrms_system.Models
{
    public class AttendanceLogViewModel
    {
        public List<DailyAttendanceViewModel> DailyRecords { get; set; } = new List<DailyAttendanceViewModel>();
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
