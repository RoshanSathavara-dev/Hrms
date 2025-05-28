namespace Hrms_system.Models
{
    public class EmployeeAttendanceViewModel
    {
        public Employee? Employee { get; set; }
        public List<EmployeeAttendanceRecordViewModel>? Records { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public EmployeeAttendanceSummary? Summary { get; set; }

        public DateTime SelectedDate { get; set; }

    }
}
