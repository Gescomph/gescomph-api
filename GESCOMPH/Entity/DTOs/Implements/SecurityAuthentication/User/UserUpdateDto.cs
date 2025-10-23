using Entity.DTOs.Base;

namespace Entity.DTOs.Implements.SecurityAuthentication.User
{
    public sealed class UserUpdateDto : BaseDto
    {
        public required int PersonId { get; init; }
        public required string Email { get; init; }
        public IReadOnlyList<int>? RoleIds { get; init; }
        public string? Password { get; init; }
        public bool? Active { get; init; }
        public bool SendPasswordByEmail { get; init; } = false;
    }
}
