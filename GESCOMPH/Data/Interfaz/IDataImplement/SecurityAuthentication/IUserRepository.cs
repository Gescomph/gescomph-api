using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.SecurityAuthentication;

namespace Data.Interfaz.IDataImplement.SecurityAuthentication
{
    public interface IUserRepository : IDataGeneric<User>
    {
        // ======== Verificación de existencia ========
        Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);

        // ======== Consultas por Email ========
        Task<int?> GetIdByEmailAsync(string email);
        Task<User?> GetByEmailAsync(string email);

        // ======== Consulta mínima para autenticación ========
        Task<User?> GetAuthUserByEmailAsync(string email);
    }
}
