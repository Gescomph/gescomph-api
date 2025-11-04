using Entity.DTOs.Base;

namespace Entity.DTOs.Implements.SecurityAuthentication.User
{
    public sealed class UserUpdateDto : BaseDto
    {

        // Persona
        public string Email { get; set; } = null!;
        public string Password{ get; set; } = null!;

        public int PersonId { get; set; }

        // Roles
        public IReadOnlyList<int>? RoleIds { get; init; }

    }
}
