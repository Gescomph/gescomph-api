using Business.Interfaces.IBusiness;
using Entity.DTOs.Implements.AdministrationSystem.CollectionSetting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Interfaces.Implements.AdministrationSystem
{
    public interface ICollectionSettingServices : IBusiness<CollectionSettingSelectDto, CollectionSettingCreateDto, CollectionSettingUpdateDto> 
    {
    }
}
