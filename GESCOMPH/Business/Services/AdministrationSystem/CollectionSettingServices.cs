using Business.Interfaces.Implements.AdministrationSystem;
using Business.Repository;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.CollectionSetting;
using MapsterMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.Services.AdministrationSystem
{
    public class CollectionSettingServices : BusinessGeneric<CollectionSettingSelectDto, CollectionSettingCreateDto, CollectionSettingUpdateDto, CollectionSetting>, ICollectionSettingServices
    {
        public CollectionSettingServices(IDataGeneric<CollectionSetting> data, IMapper mapper)
            :base(data, mapper) { }
    }
}
