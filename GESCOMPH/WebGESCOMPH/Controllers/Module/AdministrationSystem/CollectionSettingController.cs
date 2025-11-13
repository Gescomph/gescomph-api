using Business.Interfaces.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.CollectionSetting;
using Microsoft.AspNetCore.Mvc;
using WebGESCOMPH.Controllers.Base;

namespace WebGESCOMPH.Controllers.Module.AdministrationSystem
{
    [Route("api/[controller]")]
    [ApiController]
    public class CollectionSettingController : BaseController<CollectionSettingSelectDto, CollectionSettingCreateDto, CollectionSettingUpdateDto>
    {
        public CollectionSettingController(ICollectionSettingServices services, ILogger<CollectionSettingController> logger) : base(services, logger) 
        { }
    }
}
