using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hrms_system.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string?  LastName { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Department is required")]
        public string? Department { get; set; }

        [Required(ErrorMessage = "Position is required")]
        public string? Position { get; set; }

        [Required]  
        public DateTime JoinDate { get; set; }

        [Required]
        public string? Status { get; set; }

        public bool IsFirstLogin { get; set; } = true;
        public string? TemporaryPassword { get; set; }
        [Required(ErrorMessage = "CompanyId is required")]
        public int CompanyId { get; set; }

        // Navigation property
        public Company? Company { get; set; }

        // Add these new properties
        public decimal Salary { get; set; }
        public decimal? Allowances { get; set; }
        public string? EmployeeNumber { get; set; }
        public string? BankAccountNumber { get; set; }

        [Display(Name = "Bank Name")]
        public string? BankName { get; set; }


        [Display(Name = "Account Type")]
        public string? BankAccountType { get; set; } // Savings, Current, etc.

        [Display(Name = "IFSC Code")]
        public string? BankIFSCCode { get; set; }

        [Display(Name = "Branch Name")]
        public string? BankBranchName { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property for Payrolls
        public ICollection<Payroll>? Payrolls { get; set; }

        public ICollection<Payment>? Payments { get; set; }

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        public bool IsUnderNoticePeriod { get; set; } = false;

        public bool IsOnProbation { get; set; } = false;
        public bool IsOnNoticePeriod { get; set; } = false;

        public int? WorkWeekRuleId { get; set; }
        [ForeignKey("WorkWeekRuleId")]
        public WorkWeekRule? WorkWeekRule { get; set; }
    }
}
