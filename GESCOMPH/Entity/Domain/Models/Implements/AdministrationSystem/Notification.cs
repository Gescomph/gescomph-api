using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Domain.Models.ModelBase;
using Entity.Enum;

namespace Entity.Domain.Models.Implements.AdministrationSystem
{
    public class Notification : BaseModel
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationType Type { get; set; } = NotificationType.System;
        public NotificationPriority Priority { get; set; } = NotificationPriority.Info;
        public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

        public int RecipientUserId { get; set; }
        public User? RecipientUser { get; set; }

        public string? ActionRoute { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
