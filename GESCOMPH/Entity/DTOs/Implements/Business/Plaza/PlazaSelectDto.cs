using Entity.DTOs.Base;

namespace Entity.DTOs.Implements.Business.Plaza
{
    public class PlazaSelectDto : BaseDto, IPlazaDto
    {
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Location { get; set; } = null!;
        public bool Active { get; set; }

        // ✅ lista de rutas o DTOs de imágenes (según lo que maneje el frontend)
        public List<string> ImagePaths { get; set; } = [];
    }
}
