    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace Hrms_system.Models
    {
        public class SalarySlip
        {
            [Key]
            public int Id { get; set; }

            // Employee Information
            [Required]
            public string? EmployeeId { get; set; } // Foreign key to ApplicationUser

            [ForeignKey("EmployeeId")]
            public ApplicationUser? Employee { get; set; }

            [Required, StringLength(100)]
            public string? EmployeeName { get; set; }

            [Required, StringLength(50)]
            public string? Department { get; set; }

            [Required, StringLength(50)]
            public string? Designation { get; set; }

            [DataType(DataType.Date)]
            public DateTime JoiningDate { get; set; }

            [StringLength(20)]
            public string? BankAccount { get; set; }

            // Period Information
            [DataType(DataType.Date)]
            public DateTime Period { get; set; }

            [DataType(DataType.Date)]
            public DateTime IssueDate { get; set; } = DateTime.Now;

            // Earnings Components
            [Column(TypeName = "decimal(18,2)")]
            public decimal BasicSalary { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal HouseRentAllowance { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal TransportAllowance { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal MealAllowance { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal PerformanceBonus { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal OtherAllowances { get; set; } = 0;

            // Deductions Components
            [Column(TypeName = "decimal(18,2)")]
            public decimal TaxDeduction { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal SocialSecurity { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal HealthInsurance { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal ProvidentFund { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal OtherDeductions { get; set; } = 0;

            // Attendance Information
            public int PaidLeaveTaken { get; set; } = 0;
            public int SickLeaveTaken { get; set; } = 0;
            public int LeaveBalance { get; set; } = 0;
            public int WorkingDays { get; set; }
            public int PresentDays { get; set; }

            // Payment Information
            [StringLength(20)]
            public string PaymentMethod { get; set; } = "Bank Transfer";

            [StringLength(20)]
            public string PaymentStatus { get; set; } = "Pending";

            // Navigation Property for History
            public List<SalarySlipHistory> History { get; set; } = new List<SalarySlipHistory>();

            // Calculated Properties
            [NotMapped]
            public decimal TotalEarnings => BasicSalary + HouseRentAllowance + TransportAllowance +
                                          MealAllowance + PerformanceBonus + OtherAllowances;

            [NotMapped]
            public decimal TotalDeductions => TaxDeduction + SocialSecurity + HealthInsurance +
                                            ProvidentFund + OtherDeductions;

            [NotMapped]
            public decimal NetPay => TotalEarnings - TotalDeductions;
        }

        public class SalarySlipHistory
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public int SalarySlipId { get; set; } // Foreign key

            [DataType(DataType.Date)]
            public DateTime Period { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal BasicSalary { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal TotalEarnings { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal TotalDeductions { get; set; }

            [Column(TypeName = "decimal(18,2)")]
            public decimal NetPay { get; set; }

            [StringLength(20)]
            public string Status { get; set; } = "Paid";

            // Navigation property back to SalarySlip
            [ForeignKey("SalarySlipId")]
            public SalarySlip? SalarySlip { get; set; }
        }
    }