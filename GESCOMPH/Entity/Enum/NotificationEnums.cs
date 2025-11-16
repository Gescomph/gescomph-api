namespace Entity.Enum
{
    public enum NotificationType
    {
        System = 1,
        ContractCreated = 2,
        ContractExpiring = 3,
        Reminder = 4
    }

    public enum NotificationPriority
    {
        Info = 1,
        Warning = 2,
        Critical = 3
    }

    public enum NotificationStatus
    {
        Unread = 1,
        Read = 2,
        Archived = 3
    }
}
