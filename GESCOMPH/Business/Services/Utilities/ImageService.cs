using Business.Interfaces.Implements.Utilities;
using Business.Repository;
using Data.Interfaz.IDataImplement.Utilities;
using Entity.Domain.Models.Implements.Utilities;
using Entity.DTOs.Implements.Utilities.Images;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Utilities.Exceptions;
using Utilities.Helpers.CloudinaryHelper;
using Entity.Enum;


namespace Business.Services.Utilities
{
    /// <summary>
    /// Servicio para manejar imágenes con Cloudinary (polimórfico).
    /// </summary>
    public sealed class ImageService :
        BusinessGeneric<ImageSelectDto, ImageCreateDto, ImageUpdateDto, Images>, IImagesService
    {
        private readonly IImagesRepository _imagesRepository;
        private readonly CloudinaryUtility _cloudinary;
        private readonly ILogger<ImageService> _logger;

        private const int MaxParallelUploads = 3;
        private const int MaxFilesPerRequest = 5;

        public ImageService(
            IImagesRepository imagesRepository,
            CloudinaryUtility cloudinary,
            IMapper mapper,
            ILogger<ImageService> logger
        ) : base(imagesRepository, mapper)
        {
            _imagesRepository = imagesRepository;
            _cloudinary = cloudinary;
            _logger = logger;
        }

        /// <summary>
        /// Subida polimórfica → entityType = "Establishment" | "Plaza" | etc.
        /// </summary>
        public async Task<List<ImageSelectDto>> AddImagesAsync(string entityType, int entityId, IFormFileCollection files)
        {
            if (string.IsNullOrWhiteSpace(entityType))
                throw new BusinessException("entityType requerido.");

            if (entityId <= 0)
                throw new BusinessException("entityId inválido.");

            if (files is null || files.Count == 0)
                throw new BusinessException("Debe adjuntar al menos un archivo.");

            var allowedMime = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/png", "image/webp" };
            var allowedExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".webp" };

            var filesToUpload = files.Take(MaxFilesPerRequest)
                .Where(f => f?.Length > 0)
                .Where(f =>
                {
                    var ct = f.ContentType ?? string.Empty;
                    var ext = System.IO.Path.GetExtension(f.FileName) ?? string.Empty;
                    if (!allowedMime.Contains(ct) || !allowedExt.Contains(ext))
                        throw new BusinessException($"El archivo '{f.FileName}' tiene un formato no permitido. Solo se permiten JPG, PNG o WEBP.");
                    return true;
                })
                .ToList();
            if (filesToUpload.Count == 0)
                throw new BusinessException("No se recibieron archivos válidos.");

            var uploadedEntities = new List<Images>();
            var uploadedPublicIds = new List<string>();
            using var semaphore = new SemaphoreSlim(MaxParallelUploads);

            try
            {
                var tasks = filesToUpload.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Cloudinary solo recibe el id numérico, así que esto no cambia
                        var result = await _cloudinary.UploadImageAsync(file, entityId);

                        lock (uploadedEntities)
                        {
                            uploadedEntities.Add(new Images
                            {
                                FileName = file.FileName,
                                FilePath = result.SecureUrl.AbsoluteUri,
                                PublicId = result.PublicId,
                                EntityType = Enum.Parse<EntityType>(entityType, true),
                                EntityId = entityId
                            });

                            uploadedPublicIds.Add(result.PublicId);
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                await _imagesRepository.AddRangeAsync(uploadedEntities);

                _logger.LogInformation("Subidas {Count} imágenes para {EntityType} {Id}",
                    uploadedEntities.Count, entityType, entityId);

                return _mapper.Map<List<ImageSelectDto>>(uploadedEntities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Falló la subida/persistencia de imágenes para {EntityType} {Id}. Ejecutando rollback...",
                    entityType, entityId);

                var deletes = uploadedPublicIds.Select(pid => _cloudinary.DeleteAsync(pid));
                await Task.WhenAll(deletes);

                throw new BusinessException("Error al adjuntar imágenes.", ex);
            }
        }

        public async Task DeleteByPublicIdAsync(string publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
                throw new BusinessException("PublicId requerido.");

            await _cloudinary.DeleteAsync(publicId);
            await _imagesRepository.DeleteByPublicIdAsync(publicId);
        }

        public async Task DeleteByIdAsync(int id)
        {
            var img = await _imagesRepository.GetByIdAsync(id);
            if (img is null)
                return;

            await _cloudinary.DeleteAsync(img.PublicId);
            await _imagesRepository.DeleteAsync(id);
        }

        /// <summary>
        /// Polimórfico: obtiene imágenes por entidad.
        /// </summary>
        public async Task<List<ImageSelectDto>> GetImagesAsync(string entityType, int entityId)
        {
            var images = await _imagesRepository.GetByAsync(entityType, entityId);
            return _mapper.Map<List<ImageSelectDto>>(images);
        }
    }
}
