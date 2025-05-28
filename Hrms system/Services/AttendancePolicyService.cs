using Hrms_system.Models;

namespace Hrms_system.Services
{
    public class AttendancePolicyService : IAttendancePolicyService
    {
        public AttendancePolicy GetDefaultPolicy()
        {
            return new AttendancePolicy
            {
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(18, 0, 0),
                BreakDuration = TimeSpan.FromHours(1),
                LateGracePeriodMinutes = 15,
                EarlyDepartureGraceMinutes = 15,
                MaxBreaksPerDay = 2,
                MaxBreakDurationMinutes = 60
            };
        }
    }
}
