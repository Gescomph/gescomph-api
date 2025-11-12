using Business.Interfaces.IBusiness;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.User;

namespace Business.Interfaces.Implements.SecurityAuthentication
{
    public interface IUserService : IBusiness<UserSelectDto, UserCreateDto, UserUpdateDto>
    {
        Task<UserSelectDto?> GetByPersonIdAsync(int personId, CancellationToken ct = default);
    }
}
