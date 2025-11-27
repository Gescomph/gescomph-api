using Entity.DTOs.Base;
using Entity.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.DTOs.Implements.AdministrationSystem.CollectionSetting
{
    public class CollectionSettingSelectDto : BaseDto
    {
        public string Name { get; set; } = null!;
        public double Value { get; set; }
        public TimeUnit TimeUnit { get; set; }
        public string? Description { get; set; }

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
