using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Enum;

namespace Data.Interfaz.IDataImplement.AdministrationSystem
{
    public interface INotificationRepository : IDataGeneric<Notification>
    {
        Task<IReadOnlyList<Notification>> GetUnreadByUserAsync(int userId);
        Task<IReadOnlyList<Notification>> GetByUserAsync(int userId, NotificationStatus? status = null, int take = 20);
        Task<int> MarkAllAsReadAsync(int userId);

        Task<bool> HasRecentNotificationAsync(int recipientUserId, NotificationType type, string? actionRoute, DateTime since);
    }
}
