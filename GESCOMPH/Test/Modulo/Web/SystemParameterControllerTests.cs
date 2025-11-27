using Business.Interfaces.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.SystemParameter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.AdministrationSystem;

namespace Test.Modulo.Web;

public class SystemParameterControllerTests
{
    private readonly Mock<ISystemParameterService> _service = new();
    private readonly Mock<ILogger<SystemParameterController>> _logger = new();
    private SystemParameterController Create() => new(_service.Object, _logger.Object);

    [Fact]
    public async Task DeleteReturnsNoContentWhenDeleted()
    {
        _service.Setup(s => s.DeleteAsync(4)).ReturnsAsync(true);
        var res = await Create().Delete(4);
        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task GetReturnsOk()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<SystemParameterSelectDto> { new() { Id = 1 } });
        var res = await Create().Get();
        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task GetByIdNotFound()
    {
        _service.Setup(s => s.GetByIdAsync(9)).ReturnsAsync((SystemParameterSelectDto?)null);
        var res = await Create().GetById(9);
        Assert.IsType<NotFoundResult>(res.Result);
    }

    [Fact]
    public async Task PostCreatedAt()
    {
        var created = new SystemParameterSelectDto { Id = 2 };

        _service.Setup(s => s.CreateAsync(It.IsAny<SystemParameterDto>()))
                .ReturnsAsync(created);

        var res = await Create().Post(new SystemParameterDto { Key = "K", Value = "V" });

        var result = Assert.IsType<CreatedAtActionResult>(res.Result);

        Assert.Equal(201, result.StatusCode);
        Assert.Equal("GetById", result.ActionName);
        Assert.Equal(created, result.Value);
    }

    [Fact]
    public async Task PutOk()
    {
        var updated = new SystemParameterSelectDto { Id = 3 };
        _service.Setup(s => s.UpdateAsync(It.IsAny<SystemParameterUpdateDto>())).ReturnsAsync(updated);
        var res = await Create().Put(3, new SystemParameterUpdateDto { Id = 3 });
        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task DeleteLogicNoContent()
    {
        _service.Setup(s => s.DeleteLogicAsync(5)).ReturnsAsync(true);
        var res = await Create().DeleteLogic(5);
        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task ChangeActiveStatusNoContent()
    {
        var res = await Create().ChangeActiveStatus(7, new WebGESCOMPH.Contracts.Requests.ChangeActiveStatusRequest { Active = true });
        Assert.IsType<NoContentResult>(res);
    }
}
