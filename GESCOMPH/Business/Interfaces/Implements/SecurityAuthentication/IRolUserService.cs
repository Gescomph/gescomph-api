using Business.Interfaces.IBusiness;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.RolUser;

namespace Business.Interfaces.Implements.SecurityAuthentication
{
    public interface IRolUserService : IBusiness<RolUserSelectDto, RolUserCreateDto, RolUserUpdateDto>
    {
        Task<IEnumerable<string>> GetRoleNamesByUserIdAsync(int userId);
        Task ReplaceUserRolesAsync(int userId, IEnumerable<int> roleIds);
        Task<RolUser> AsignateRolDefaultAsync(User user);
    }
}
