using Business.Interfaces.Implements.Utilities;
using Entity.DTOs.Implements.Utilities.Images;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebGESCOMPH.Controllers.Module.Utilities
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public sealed class ImagesController : ControllerBase
    {
        private readonly IImagesService _imagesService;

        public ImagesController(IImagesService imagesService)
        {
            _imagesService = imagesService;
        }

        /// <summary>
        /// Adjunta imágenes a una entidad (polimórfico).
        /// </summary>
        [HttpPost("{entityType}/{entityId:int}")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(List<ImageSelectDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<ImageSelectDto>>> Upload(
            string entityType,
            int entityId,
            [FromForm] IFormFileCollection files)
        {
            if (files is null || files.Count == 0)
                return BadRequest(new { detail = "Debe adjuntar al menos un archivo." });

            var result = await _imagesService.AddImagesAsync(entityType, entityId, files);
            return Ok(result);
        }

        /// <summary>
        /// Obtiene imágenes por entidad.
        /// </summary>
        [HttpGet("{entityType}/{entityId:int}")]
        [ProducesResponseType(typeof(List<ImageSelectDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<ImageSelectDto>>> GetImages(string entityType, int entityId)
        {
            var images = await _imagesService.GetImagesAsync(entityType, entityId);
            return Ok(images);
        }

        /// <summary>
        /// Elimina una imagen por PublicId.
        /// </summary>
        [HttpDelete("public/{publicId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Delete(string publicId)
        {
            await _imagesService.DeleteByPublicIdAsync(publicId);
            return NoContent();
        }

        /// <summary>
        /// Elimina una imagen por Id numérico.
        /// </summary>
        [HttpDelete("id/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> DeleteById(int id)
        {
            await _imagesService.DeleteByIdAsync(id);
            return NoContent();
        }
    }
}
