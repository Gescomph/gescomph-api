using Entity.DTOs.Base;

namespace Entity.DTOs.Implements.Business.Clause
{
    public class ClauseUpdateDto : BaseDto
    {

        public string Name { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }
    }
}
