namespace Hrms_system.Models
{
    public class Holiday
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime Date { get; set; }
        public int CompanyId { get; set; } // Add this property
        public Company? Company { get; set; }
    }
}
