namespace Entity.DTOs.Implements.Business.Plaza
{
    public class PlazaCardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public bool Active { get; set; }
        public string? PrimaryImagePath { get; set; }
    }
}
