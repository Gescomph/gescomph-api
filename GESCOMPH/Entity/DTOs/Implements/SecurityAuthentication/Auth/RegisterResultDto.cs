using System.Collections.Generic;

namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class RegisterResultDto
    {
        public string Email { get; set; } = null!;
        public int PersonId { get; set; }
        public IReadOnlyCollection<int>? RoleIds { get; set; }
    }
}
