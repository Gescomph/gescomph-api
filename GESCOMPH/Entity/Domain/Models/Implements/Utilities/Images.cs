using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.ModelBase;
using Entity.Enum;

namespace Entity.Domain.Models.Implements.Utilities
{
    public class Images : BaseModel
    {
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string PublicId { get; set; } = null!;

        //  Relación    
        public EntityType EntityType { get; set; } // "Establishment", "Plaza", ...
        public int EntityId { get; set; }
    }

}
