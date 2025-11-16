using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.SecurityAuthentication;

namespace Data.Interfaz.IDataImplement.SecurityAuthentication
{
    public interface ITwoFactorCodeRepository : IDataGeneric<TwoFactorCode>
    {
        Task<TwoFactorCode?> GetValidCodeAsync(int userId, string code);
        Task<TwoFactorCode?> GetLatestActiveCodeAsync(int userId);
        Task InvalidatePendingCodesAsync(int userId);
    }
}
