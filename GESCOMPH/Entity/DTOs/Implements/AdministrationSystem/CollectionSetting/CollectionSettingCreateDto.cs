using Entity.DTOs.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.DTOs.Implements.AdministrationSystem.CollectionSetting
{
    public class CollectionSettingCreateDto : BaseDto
    {
        public string Name { get; set; } = null!;
        public double Value { get; set; }
        public int TimeUnit { get; set; }
        public string? Description { get; set; }
    }
}
