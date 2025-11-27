using Entity.Domain.Models.ModelBase;
using Entity.Enum;
using System.ComponentModel.DataAnnotations.Schema;


namespace Entity.Domain.Models.Implements.AdministrationSystem
{
    public class CollectionSetting : BaseModelGeneric
    {
        public double Value { get; set; }
        public TimeUnit TimeUnit { get; set; }

        [NotMapped]
        public TimeSpan TimeSpan => TimeUnit switch
        {
            TimeUnit.Seconds => TimeSpan.FromSeconds(Value),
            TimeUnit.Minutes => TimeSpan.FromMinutes(Value),
            TimeUnit.Hours => TimeSpan.FromHours(Value),
            TimeUnit.Days => TimeSpan.FromDays(Value),
            _ => TimeSpan.Zero
        };
    }
}
