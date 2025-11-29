using Business.Interfaces.Implements.Business;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebGESCOMPH.Controllers.Module.Business;

namespace Test.Modulo.Web;

public class AppointmentControllerTests
{
    private readonly Mock<IAppointmentService> _svc = new();

    // --------------------------------------------------------
    // GET ALL
    // --------------------------------------------------------
    [Fact]
    public async Task Get_ReturnsOk()
    {
        _svc.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto>());

        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.Get();

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.IsAssignableFrom<IEnumerable<Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto>>(ok.Value);
    }

    // --------------------------------------------------------
    // GET BY ID
    // --------------------------------------------------------
    [Fact]
    public async Task GetById_NotFound()
    {
        _svc.Setup(s => s.GetByIdAsync(9))
            .ReturnsAsync((Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto?)null);

        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.GetById(9);

        Assert.IsType<NotFoundObjectResult>(res.Result);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        _svc.Setup(s => s.GetByIdAsync(5))
            .ReturnsAsync(new Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto { Id = 5 });

        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.GetById(5);

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var dto = Assert.IsType<Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto>(ok.Value);
        Assert.Equal(5, dto.Id);
    }

    // --------------------------------------------------------
    // CREATE
    // --------------------------------------------------------
    [Fact]
    public async Task Create_ReturnsOk()
    {
        _svc.Setup(s => s.CreateAsync(It.IsAny<Entity.DTOs.Implements.Business.Appointment.AppointmentCreateDto>()))
            .ReturnsAsync(new Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto { Id = 1 });

        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.Create(
            new Entity.DTOs.Implements.Business.Appointment.AppointmentCreateDto 
            { 
                FirstName = "A", 
                LastName = "B", 
                Document = "1", 
                Address = "X", 
                Phone = "Y", 
                EstablishmentId = 1, 
                Description = "D", 
                RequestDate = DateTime.UtcNow 
            });

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        Assert.IsType<Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto>(ok.Value);
    }

    // --------------------------------------------------------
    // UPDATE
    // --------------------------------------------------------
    [Fact]
    public async Task Update_BadRequest_WhenMismatchId()
    {
        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.Update(2, new Entity.DTOs.Implements.Business.Appointment.AppointmentUpdateDto { Id = 1 });

        Assert.IsType<BadRequestObjectResult>(res.Result);
    }


    [Fact]
    public async Task Update_ReturnsOk_WhenUpdated()
    {
        _svc.Setup(s => s.UpdateAsync(It.IsAny<Entity.DTOs.Implements.Business.Appointment.AppointmentUpdateDto>()))
            .ReturnsAsync(new Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto { Id = 1 });

        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.Update(1, new Entity.DTOs.Implements.Business.Appointment.AppointmentUpdateDto { Id = 1 });

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var dto = Assert.IsType<Entity.DTOs.Implements.Business.Appointment.AppointmentSelectDto>(ok.Value);
        Assert.Equal(1, dto.Id);
    }

    // --------------------------------------------------------
    // DELETE
    // --------------------------------------------------------
    [Fact]
    public async Task Delete_Ok()
    {
        var ctrl = new AppointmentController(_svc.Object);

        var res = await ctrl.Delete(1);

        Assert.IsType<OkResult>(res);
    }
}
