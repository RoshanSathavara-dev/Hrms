namespace Hrms_system.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool Seen { get; set; } = false;
        public string? UserId { get; set; } // Who triggered the notification
        public virtual ApplicationUser User { get; set; } // Navigation property
    }
}
