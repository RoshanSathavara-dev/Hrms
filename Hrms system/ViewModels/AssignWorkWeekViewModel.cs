using System.Collections.Generic;
using Hrms_system.Models;

namespace Hrms_system.ViewModels
{
    public class AssignWorkWeekViewModel
    {
        public List<EmployeeWorkWeekViewModel>? Employees { get; set; }
        public List<WorkWeekRule>? WorkWeekRules { get; set; }
    }

    public class EmployeeWorkWeekViewModel
    {
        public int Id { get; set; }
        // Removed EmployeeCode
        public string? FullName { get; set; }
        public string? Department { get; set; }
        // Removed Location
        // Removed Type
        public string? AssignedWorkWeekRuleName { get; set; }
    }
}