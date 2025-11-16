namespace Entity.DTOs.Implements.SecurityAuthentication.User
{
    public class UserCreateDto
    {
        // Persona
        public string Email { get; set; } = null!;
        public string Password{ get; set; } = null!;

        public int PersonId { get; set; }

        // Roles
        public IReadOnlyList<int>? RoleIds { get; init; }
    }
}
