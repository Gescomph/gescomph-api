using Entity.DTOs.Base;
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
        public string TimeUnitName { get; set; } = null!; // Aquí quieres el nombre
        public string? Description { get; set; }
    }
}
