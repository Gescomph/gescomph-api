using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Utilities;

namespace Data.Interfaz.IDataImplement.Utilities
{
    public interface IImagesRepository : IDataGeneric<Images>
    {
        Task AddRangeAsync(IEnumerable<Images> images);
        Task<bool> DeleteByPublicIdAsync(string publicId);
        Task<bool> DeleteLogicalByPublicIdAsync(string publicId);

        Task<List<Images>> GetByAsync(string entityType, int entityId);
    }
}
