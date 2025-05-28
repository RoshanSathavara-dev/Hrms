using Hrms_system.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hrms_system.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
          : base(options)
        {
        }

        // ✅ Add Attendance Table
        public DbSet<Attendance> Attendance { get; set; }
        public DbSet<LeaveRequest> LeaveRequests { get; set; }

        public DbSet<SalarySlip> SalarySlips { get; set; }
        public DbSet<SalarySlipHistory> SalarySlipHistories { get; set; }

        public DbSet<Employee> Employees { get; set; }

        public DbSet<Company> Companies { get; set; }

        public DbSet<Payroll> Payrolls { get; set; }
        public DbSet<PayrollRun> PayrollRuns { get; set; }

        public DbSet<Payment> Payments { get; set; }

        public DbSet<Holiday> Holidays { get; set; }

        public DbSet<AttendancePolicy> AttendancePolicies { get; set; }


        public DbSet<BreakLog> BreakLogs { get; set; }

        public DbSet<Notification> Notifications { get; set; }


        public DbSet<LeaveType> LeaveTypes { get; set; }

        public DbSet<EmployeeLeaveBalance> EmployeeLeaveBalances { get; set; }

        public DbSet<LeaveBalanceHistory> LeaveBalanceHistories { get; set; }

        public DbSet<LeaveAssignment> LeaveAssignments { get; set; }




        // In ApplicationDbContext.cs





        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Ensure Identity tables are set up

            modelBuilder.Entity<Attendance>()
                .HasOne(lr => lr.User)
                .WithMany()  // No direct navigation property in IdentityUser
                .HasForeignKey(a => a.UserId)
                .IsRequired()  // Ensures that UserId is mandatory
                .OnDelete(DeleteBehavior.Restrict); // Prevent accidental deletion of attendance records



            modelBuilder.Entity<LeaveRequest>()
                .HasOne(a => a.User)
                .WithMany() // No navigation property in IdentityUser
                .HasForeignKey(lr => lr.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.Property(lr => lr.Days)
                    .HasPrecision(10, 2); // 10 total digits, 2 decimal places
            });

            // SalarySlip configuration
            modelBuilder.Entity<SalarySlip>()
                .HasIndex(s => new { s.EmployeeId, s.Period })
                .IsUnique();

            // SalarySlipHistory configuration
            modelBuilder.Entity<SalarySlipHistory>()
                .HasOne(h => h.SalarySlip)
                .WithMany(s => s.History)
                .HasForeignKey(h => h.SalarySlipId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>()
                .HasOne(e => e.Company)
                .WithMany(c => c.Employees)
                .HasForeignKey(e => e.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Employee>(entity =>
            {
                entity.Property(e => e.Salary).HasPrecision(18, 2);
                entity.Property(e => e.Allowances).HasPrecision(18, 2);
            });

            modelBuilder.Entity<Payroll>(entity =>
            {
                entity.Property(p => p.BasicSalary).HasPrecision(18, 2);
                entity.Property(p => p.Allowances).HasPrecision(18, 2);
                entity.Property(p => p.Deductions).HasPrecision(18, 2);
                entity.Property(p => p.NetPay).HasPrecision(18, 2);

                entity.HasIndex(p => new { p.Period, p.EmployeeId }).IsUnique();

                entity.HasOne(p => p.Employee)
                    .WithMany(e => e.Payrolls)
                    .HasForeignKey(p => p.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<LeaveAssignment>(entity =>
            {
                entity.HasOne(la => la.Employee)
                    .WithMany()
                    .HasForeignKey(la => la.EmployeeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(la => la.LeaveType)
                    .WithMany()
                    .HasForeignKey(la => la.LeaveTypeId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(la => new { la.EmployeeId, la.LeaveTypeId, la.IsActive })
                    .HasFilter("[IsActive] = 1")
                    .IsUnique();
            });
            // Configure decimal precision for PayrollRun
            modelBuilder.Entity<PayrollRun>(entity =>
            {
                entity.Property(p => p.TotalGross).HasPrecision(18, 2);
                entity.Property(p => p.TotalNet).HasPrecision(18, 2);

                entity.HasIndex(p => p.Period);
                entity.HasIndex(p => p.RunDate);
                entity.HasIndex(p => p.Status);
            });



            modelBuilder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasPrecision(18, 2);
            });

            modelBuilder.Entity<EmployeeLeaveBalance>(entity =>
            {
                entity.Property(e => e.TotalLeaves).HasPrecision(10, 2);
                entity.Property(e => e.UsedLeaves).HasPrecision(10, 2);
                entity.Property(e => e.PendingLeaves).HasPrecision(10, 2);
                entity.Property(e => e.CarryForwardedLeaves).HasPrecision(10, 2);
            });

            // Configure decimal precision for LeaveType
            modelBuilder.Entity<LeaveType>(entity =>
            {
                entity.Property(e => e.LeavesAllowedPerYear).HasPrecision(10, 2);
                entity.Property(e => e.CarryForwardLimit).HasPrecision(10, 2);
            });

            modelBuilder.Entity<LeaveBalanceHistory>(entity =>
            {
                entity.Property(e => e.PreviousTotal)
                      .HasPrecision(10, 2);  // 10 total digits, 2 decimal places

                entity.Property(e => e.NewTotal)
                      .HasPrecision(10, 2);  // 10 total digits, 2 decimal places
            });

            modelBuilder.Entity<BreakLog>()
    .HasOne(b => b.Attendance)
    .WithMany(a => a.BreakLogs)
    .HasForeignKey(b => b.AttendanceId)
    .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
