namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    public class RegisterDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;

        // Datos personales
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        //public DocumentType DocumentType { get; set; }
        public string Document { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public int CityId { get; set; }
        public IReadOnlyList<int>? RoleIds { get; set; }
        public bool SendPasswordByEmail { get; set; } = true;
    }
}
