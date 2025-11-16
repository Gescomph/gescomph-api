using System;
using Entity.DTOs.Base;
using Entity.Enum;

namespace Entity.DTOs.Implements.Utilities
{
    public class NotificationDto : BaseDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public NotificationStatus Status { get; set; }
        public int RecipientUserId { get; set; }
        public string? ActionRoute { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class NotificationCreateDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public int RecipientUserId { get; set; }
        public string? ActionRoute { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class NotificationUpdateDto : BaseDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public NotificationType Type { get; set; }
        public NotificationPriority Priority { get; set; }
        public NotificationStatus Status { get; set; }
        public int RecipientUserId { get; set; }
        public string? ActionRoute { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}