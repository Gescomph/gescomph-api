using System.Threading.Tasks;

namespace Business.Interfaces.Implements.Business
{
    public interface IObligationNotifier
    {
        Task NotifyObligationsUpdatedAsync();
    }
}
