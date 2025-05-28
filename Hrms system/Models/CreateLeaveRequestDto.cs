using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class CreateLeaveRequestDto
    {
        [Required(ErrorMessage = "Leave type is required")]
        public int LeaveTypeId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Reason is required")]
        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }

        public bool IsHalfDayStart { get; set; } // Indicates if start date is half-day
        public bool IsFirstHalfStart { get; set; } // True for First Half, False for Second Half for start date

        public bool IsHalfDayEnd { get; set; } // Indicates if end date is half-day
        public bool IsFirstHalfEnd { get; set; }

        [Required(ErrorMessage = "Start half selection is required")]
        public string? StartHalf { get; set; } // "First" or "Second"

        [Required(ErrorMessage = "End half selection is required")]
        public string? EndHalf { get; set; } // "First" or "Second"


    }
}
