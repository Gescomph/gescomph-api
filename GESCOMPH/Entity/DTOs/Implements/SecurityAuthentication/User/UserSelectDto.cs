using Entity.DTOs.Base;

namespace Entity.DTOs.Implements.SecurityAuthentication.User
{
    public class UserSelectDto : BaseDto
    {
        public int PersonId { get; set; }
        public string PersonFirstName { get; set; } = string.Empty;
        public string PersonLastName { get; set; } = string.Empty;
        public string PersonName { get; set; } = string.Empty;
        public string PersonDocument { get; set; } = string.Empty;
        public string PersonAddress { get; set; } = string.Empty;
        public string PersonPhone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
    }
}
