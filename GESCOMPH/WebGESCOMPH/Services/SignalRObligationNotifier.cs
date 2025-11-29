using Business.Interfaces.Implements.Business;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using WebGESCOMPH.RealTime.Obligations;

namespace WebGESCOMPH.Services
{
    public class SignalRObligationNotifier : IObligationNotifier
    {
        private readonly IHubContext<ObligationHub> _hub;

        public SignalRObligationNotifier(IHubContext<ObligationHub> hub)
        {
            _hub = hub;
        }

        public async Task NotifyObligationsUpdatedAsync()
        {
            await _hub.Clients.All.SendAsync("ObligationsUpdated");
        }
    }
}
