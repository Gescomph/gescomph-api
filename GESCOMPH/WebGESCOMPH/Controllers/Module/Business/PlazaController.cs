using Business.Interfaces.Implements.Business;
using Business.Services.Business;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.DTOs.Implements.Business.Plaza;
using Microsoft.AspNetCore.Mvc;
using Utilities.Exceptions;
using WebGESCOMPH.Contracts.Requests;
using WebGESCOMPH.Controllers.Base;

namespace WebGESCOMPH.Controllers.Module.Business
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize]
    public sealed class PlazaController :
        BaseController<PlazaSelectDto, PlazaCreateDto, PlazaUpdateDto>
    {
        private readonly IPlazaService _plazaService;
        private readonly ILogger<PlazaController> _logger;

        public PlazaController(
            IPlazaService service,
            ILogger<PlazaController> logger
        ) : base(service, logger)
        {
            _plazaService = service;
            _logger = logger;
        }


        /// <summary>
        /// Listado liviano para tarjetas/grillas. Incluye PrimaryImagePath pero no la coleccion completa de imagenes.
        /// </summary>
        [HttpGet("cards")]
        [ProducesResponseType(typeof(IEnumerable<PlazaCardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCards()
        {
            var result =  await _plazaService.GetCardsAnyAsync();
            return Ok(result);
                //? await _plazaService.GetCardsActiveAsync()
        }


        // único override necesario: PATCH estado (porque tu lógica es diferente)
        [HttpPatch("{id:int}/estado")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public override async Task<IActionResult> ChangeActiveStatus(
            int id,
            [FromBody] ChangeActiveStatusRequest body)
        {
            try
            {
                await _plazaService.UpdateActiveStatusAsync(id, body.Active!.Value);
                return NoContent();
            }
            catch (BusinessException ex)
            {
                return Ok(new { success = false, message = ex.Message });
            }
        }
    }
}
