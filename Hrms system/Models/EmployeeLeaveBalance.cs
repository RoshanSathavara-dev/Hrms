using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class EmployeeLeaveBalance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveTypeId { get; set; }
        private decimal _totalLeaves;
        public decimal TotalLeaves
        {
            get => _totalLeaves;
            set => _totalLeaves = value >= 0 ? value : 0;
        }

        private decimal _usedLeaves;
        public decimal UsedLeaves
        {
            get => _usedLeaves;
            set => _usedLeaves = value >= 0 ? value : 0;
        }

        private decimal _pendingLeaves;
        public decimal PendingLeaves
        {
            get => _pendingLeaves;
            set => _pendingLeaves = value >= 0 ? value : 0;
        }
        public decimal? CarryForwardedLeaves { get; set; }
        //public DateTime? CarryForwardExpiry { get; set; }

        [NotMapped]
        public decimal AvailableLeaves => TotalLeaves - UsedLeaves - PendingLeaves + (CarryForwardedLeaves ?? 0);

        public Employee? Employee { get; set; }
        public LeaveType? LeaveType { get; set; }
        public int Year { get; set; }

        public DateTime? LastAccrualDate { get; set; }
    }
}
