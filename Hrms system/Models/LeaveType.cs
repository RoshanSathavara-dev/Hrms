using System.ComponentModel.DataAnnotations;

namespace Hrms_system.Models
{
    public class LeaveType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }
        public int CompanyId { get; set; }

        // Leaves Count
        [Required]
        [Range(0, 100, ErrorMessage = "Leaves allowed per year must be between 0 and 100.")]
        public decimal LeavesAllowedPerYear { get; set; }
        public bool ConsiderHolidaysBetweenLeave { get; set; }
        public bool ConsiderWeekendsBetweenLeave { get; set; }

        // Accrual
        public bool IsCreditableOnAccrualBasis { get; set; }


        [StringLength(50)]
        public string? AccrualFrequency { get; set; } // Monthly, Quarterly, Yearly
        //public bool IsCreditableOnPresentDayBasis { get; set; }
        //public string? AccrualPeriod { get; set; } // Start, End

        // Applicability
        public bool AllowedUnderProbation { get; set; }
        public bool AllowedUnderNoticePeriod { get; set; }
        public bool LeaveEncashEnabled { get; set; }

        // Carry Forward
        public bool CarryForwardEnabled { get; set; }
        [Range(0, 100, ErrorMessage = "Carry forward limit must be between 0 and 100.")]
        public decimal? CarryForwardLimit { get; set; }

        //[Range(0, 36, ErrorMessage = "Expiry must be between 0 and 36 months.")]
        //public int? CarryForwardExpiryInMonths { get; set; }
        public bool StartAccrualFromCurrentDate { get; set; }

        public bool CreditOnFirstDayOfMonth { get; set; }
        public bool IsLossOfPay { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
