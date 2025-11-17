using Entity.DTOs.Base;
using Entity.Enum;

namespace Entity.DTOs.Implements.Utilities.Images
{
    public class ImageSelectDto : BaseDto
    {
        public ImageSelectDto(int id, string fileName, string filePath, string publicId,
                              EntityType entityType, int entityId)
        {
            Id = id;
            FileName = fileName;
            FilePath = filePath;
            PublicId = publicId;
            EntityType = entityType;
            EntityId = entityId;
        }

        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string PublicId { get; set; } = null!;

        public EntityType EntityType { get; set; }
        public int EntityId { get; set; }
    }
}
