using Business.CustomJWT;
using Microsoft.AspNetCore.SignalR;

namespace WebGESCOMPH.RealTime.Notifications
{
    /// <summary>
    /// Hub de SignalR encargado de gestionar las conexiones en tiempo real para
    /// las notificaciones internas de la aplicación. Cada conexión autenticada
    /// se agrupa por identificador de usuario para permitir el push dirigido.
    /// </summary>
    public class NotificationsHub : Hub
    {
        private readonly ICurrentUser _currentUser;

        public NotificationsHub(ICurrentUser currentUser)
        {
            _currentUser = currentUser;
        }

        /// <summary>
        /// Durante la conexión, agrega al usuario autenticado al grupo
        /// <c>user-{{userId}}</c> para que solo reciba sus notificaciones.
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = _currentUser.UserId;
            if (userId.HasValue && userId.Value > 0)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId.Value}");
            }

            await base.OnConnectedAsync();
        }
    }
}