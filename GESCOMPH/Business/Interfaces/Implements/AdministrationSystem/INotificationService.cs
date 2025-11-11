using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.Utilities;
using Entity.Enum;

namespace Business.Interfaces.Implements.AdministrationSystem
{
    public interface INotificationService
        : IBusiness<NotificationDto, NotificationCreateDto, NotificationUpdateDto>
    {
        Task<IReadOnlyList<NotificationDto>> GetFeedAsync(int userId, NotificationStatus? status = null, int take = 20);
        Task<IReadOnlyList<NotificationDto>> GetUnreadAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<int> MarkAllAsReadAsync(int userId);
    }
}
