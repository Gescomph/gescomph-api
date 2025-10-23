namespace Entity.DTOs.Implements.SecurityAuthentication.User
{
    public class UserCreateDto
    {
        public required int PersonId { get; init; }
        public required string Email { get; init; }
        public string? Password { get; init; }
        public IReadOnlyList<int>? RoleIds { get; init; }
        public bool SendPasswordByEmail { get; init; } = true;
    }
}
