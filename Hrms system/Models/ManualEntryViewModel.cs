using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class ManualEntryViewModel
    {
        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Date")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required]
        [Display(Name = "Clock In Time")]
        public string ClockInTime { get; set; } = "09:00";

        [DataType(DataType.Time)]
        [Display(Name = "Clock Out Time")]
        public string? ClockOutTime { get; set; } 


    }
}
