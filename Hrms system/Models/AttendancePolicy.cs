using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class AttendancePolicy
    {
        public int Id { get; set; }

        [Required]
        public int CompanyId { get; set; }

        // Private backing fields
        private TimeSpan _startTime = new TimeSpan(9, 0, 0); // Default 9:00 AM
        private TimeSpan _endTime = new TimeSpan(18, 0, 0);   // Default 6:00 PM
        private TimeSpan _breakDuration = TimeSpan.FromHours(1); // Default 1 hour break

        [Required]
        [Display(Name = "Start Time")]
        public TimeSpan StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                ExpectedWorkHours = CalculateExpectedHours();
            }
        }

        [Required]
        [Display(Name = "End Time")]
        public TimeSpan EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                ExpectedWorkHours = CalculateExpectedHours();
            }
        }

        [Required]
        [Display(Name = "Break Duration")]
        public TimeSpan BreakDuration
        {
            get => _breakDuration;
            set
            {
                _breakDuration = value;
                ExpectedWorkHours = CalculateExpectedHours();
            }
        }

        [Required]
        [Display(Name = "Expected Work Hours")]
        public TimeSpan ExpectedWorkHours { get; set; }


  

        [Display(Name = "Late Grace Period (minutes)")]
        [Range(0, 120)]
        public int LateGracePeriodMinutes { get; set; } = 15; // Default 15 minutes

        [Display(Name = "Early Departure Grace (minutes)")]
        [Range(0, 120)]
        public int EarlyDepartureGraceMinutes { get; set; } = 15; // Default 15 minutes

        [Display(Name = "Max Breaks Per Day")]
        [Range(0, 5)]
        public int MaxBreaksPerDay { get; set; } = 2; // Default 2 breaks

        [Display(Name = "Max Break Duration (minutes)")]
        [Range(0, 240)]
        public int MaxBreakDurationMinutes { get; set; } = 60; // Default 60 minutes

        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string? UpdatedBy { get; set; }

        private TimeSpan CalculateExpectedHours()
        {
            return (_endTime - _startTime) - _breakDuration;
        }
    }
}
