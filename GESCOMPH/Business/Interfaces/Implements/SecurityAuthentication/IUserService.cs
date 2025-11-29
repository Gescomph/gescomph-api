using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.SecurityAuthentication.User;

namespace Business.Interfaces.Implements.SecurityAuthentication
{
    public interface IUserService : IBusiness<UserSelectDto, UserCreateDto, UserUpdateDto>
    {
        Task SoftDeleteUserAndPersonAsync(int userId);
        Task<UserSelectDto?> GetByPersonIdAsync(int personId, CancellationToken ct = default);
    }
}
