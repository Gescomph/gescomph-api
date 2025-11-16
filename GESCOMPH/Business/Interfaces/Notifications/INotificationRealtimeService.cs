using Entity.DTOs.Implements.Utilities;

namespace Business.Interfaces.Notifications
{
    /// <summary>
    /// Abstracción para enviar notificaciones en tiempo real a los clientes.
    /// Permite desacoplar la capa de negocio de la infraestructura SignalR.
    /// </summary>
    public interface INotificationRealtimeService
    {
        Task PushAsync(NotificationDto notification);
    }
}
