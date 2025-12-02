using Business.Interfaces.Implements.Business;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebGESCOMPH.Controllers.Module.Business
{
    [Route("api/[controller]")]
    //[Authorize]
    [ApiController]
    public class AppointmentController : Controller
    {

        private readonly IAppointmentService _appointmentService;

        public AppointmentController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet]

        public async Task<IActionResult> Get()
        {
            var appointments = await _appointmentService.GetAllAsync();
            return Ok(appointments);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppointmentSelectDto>> GetById(int id)
        {
            var appointment = await _appointmentService.GetByIdAsync(id);

            if (appointment == null)
                return NotFound($"Appointment con Id {id} no encontrado");

            return Ok(appointment);
        }

        [HttpGet("GetByDate")]
        public async Task<ActionResult<AppointmentSelectDto>> GetByDate(DateOnly date) 
        {
            var appointment = await _appointmentService.GetAppointmentByDate(date);
            return Ok(appointment);
        }

        [HttpGet("GetByPersonId")]
        public async Task<ActionResult<AppointmentSelectDto>> GetByPersonId(int personId)
        {
            var appointment = await _appointmentService.GetAllByPersonId(personId);
            return Ok(appointment);
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentCreateDto>> Create([FromBody] AppointmentCreateDto dto)
        {
            var appointment = await _appointmentService.CreateAsync(dto);
            return Ok(appointment);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AppointmentSelectDto>> Update(int id, [FromBody] AppointmentUpdateDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID en URL no coincide con el DTO");

            var appointment = await _appointmentService.UpdateAsync(dto);
            return Ok(appointment);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _appointmentService.DeleteAsync(id);
            return Ok();
        }

        [HttpPost("{id:int}/accept")]
        public async Task<ActionResult<AppointmentSelectDto>> AcceptAppointment(int id)
        {
            try
            {
                var appointment = await _appointmentService.UpdateStatusAsync(id, Status.Aprobada, null);
                return Ok(appointment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al aceptar la cita: {ex.Message}");
            }
        }

        [HttpPost("{id:int}/reject")]
        public async Task<ActionResult<AppointmentSelectDto>> RejectAppointment(int id, [FromBody] AppointmentRejectDto dto)
        {
            try
            {
                if (id != dto.Id)
                    return BadRequest("ID en URL no coincide con el DTO");

                var appointment = await _appointmentService.UpdateStatusAsync(id, Status.Rechazada, dto.Observation);
                return Ok(appointment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al rechazar la cita: {ex.Message}");
            }
        }

        [HttpPost("{id:int}/status")]
        public async Task<ActionResult<AppointmentSelectDto>> UpdateStatus(int id, [FromBody] AppointmentStatusDto dto)
        {
            if (id != dto.Id)
                return BadRequest("ID en URL no coincide con el DTO");

            try
            {
                var appointment = await _appointmentService.UpdateStatusAsync(id, dto.Status, dto.Observation);
                return Ok(appointment);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al actualizar el estado de la cita: {ex.Message}");
            }
        }

    }
}
