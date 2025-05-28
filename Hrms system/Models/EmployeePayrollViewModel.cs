namespace Hrms_system.Models
{
    public class EmployeePayrollViewModel
    {
        public Employee? Employee { get; set; }
        public Payroll? Payroll { get; set; }
        public bool HasSalaryConfigured { get; set; }
    }
}
