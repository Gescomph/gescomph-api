using Business.Interfaces.Notifications;
using Entity.DTOs.Implements.Utilities;
using Microsoft.AspNetCore.SignalR;

namespace WebGESCOMPH.RealTime.Notifications
{
    /// <summary>
    /// Implementación de <see cref="INotificationRealtimeService"/> basada en SignalR.
    /// Envía las notificaciones a los grupos <c>user-{{id}}</c> gestionados por <see cref="NotificationsHub"/>.
    /// </summary>
    public class SignalRNotificationRealtimeService : INotificationRealtimeService
    {
        private readonly IHubContext<NotificationsHub> _hub;

        public SignalRNotificationRealtimeService(IHubContext<NotificationsHub> hub)
        {
            _hub = hub;
        }

        public Task PushAsync(NotificationDto notification)
        {
            return _hub.Clients
                .Group($"user-{notification.RecipientUserId}")
                .SendAsync("notifications:new", notification);
        }
    }
}
