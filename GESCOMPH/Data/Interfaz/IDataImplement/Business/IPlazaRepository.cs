using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.Utilities;
using Entity.DTOs.Implements.Business.Plaza;
using Entity.Enum;

namespace Data.Interfaz.IDataImplement.Business
{
    public interface IPlazaRepository : IDataGeneric<Entity.Domain.Models.Implements.Business.Plaza>
    {

        Task<List<(Plaza plaza, List<Images> images)>> GetAllWithImagesAsync();
        Task<(Plaza? plaza, List<Images> images)> GetByIdWithImagesAsync(int id);
        Task<IReadOnlyList<PlazaCardDto>> GetCardsAsync();
    }
}
