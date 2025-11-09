using Entity.Domain.Models.ModelBase;

namespace Entity.Domain.Models.Implements.Business
{
    public class Plaza : BaseModelGeneric
    {
        public string Location { get; set; } = null!;
        public List<Establishment> Establishments { get; set; } = [];
    }
}
