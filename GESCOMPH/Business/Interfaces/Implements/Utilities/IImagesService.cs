using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.Utilities.Images;
using Microsoft.AspNetCore.Http;

namespace Business.Interfaces.Implements.Utilities
{
    /// <summary>
    /// Servicio de imágenes POLIMÓRFICO.
    /// </summary>
    public interface IImagesService : IBusiness<ImageSelectDto, ImageCreateDto, ImageUpdateDto>
    {
        /// <summary>Sube y asocia imágenes a una entidad del dominio.</summary>
        Task<List<ImageSelectDto>> AddImagesAsync(string entityType, int entityId, IFormFileCollection files);

        /// <summary>Obtiene imágenes asociadas a una entidad.</summary>
        Task<List<ImageSelectDto>> GetImagesAsync(string entityType, int entityId);

        /// <summary>Elimina una imagen por su PublicId en Cloudinary.</summary>
        Task DeleteByPublicIdAsync(string publicId);

        /// <summary>Elimina una imágen por su Id.</summary>
        Task DeleteByIdAsync(int id);
    }
}
