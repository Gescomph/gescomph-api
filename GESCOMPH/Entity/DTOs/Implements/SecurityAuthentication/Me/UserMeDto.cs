namespace Entity.DTOs.Implements.SecurityAuthentication.Me
{
    public class UserMeDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public bool TwoFactorEnabled { get; set; }

        public int PersonId { get; set; }
        public IEnumerable<string> Roles { get; set; } = [];
        //public IEnumerable<string> Permissions { get; set; } = [];

        public IEnumerable<MenuModuleDto> Menu { get; set; } = [];
    }



}