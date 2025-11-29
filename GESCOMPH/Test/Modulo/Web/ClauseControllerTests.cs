using Business.Interfaces.Implements.Business;
using Entity.DTOs.Implements.Business.Clause;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.Business;

namespace Test.Modulo.Web;

public class ClauseControllerTests
{
    private readonly Mock<IClauseService> _service = new();
    private readonly Mock<ILogger<ClauseController>> _logger = new();
    private ClauseController Create() => new(_service.Object, _logger.Object);

    [Fact]
    public async Task GetReturnsOk()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ClauseSelectDto> { new() { Id = 1, Name = "N" } });
        var res = await Create().Get();
        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task GetByIdOkWhenFound()
    {
        _service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new ClauseSelectDto { Id = 1, Name = "N" });
        var res = await Create().GetById(1);
        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task GetByIdNotFoundWhenMissing()
    {
        _service.Setup(s => s.GetByIdAsync(9)).ReturnsAsync((ClauseSelectDto?)null);
        var res = await Create().GetById(9);
        Assert.IsType<NotFoundResult>(res.Result);
    }

    [Fact]
    public async Task PutReturnsOk()
    {
        var update = new ClauseUpdateDto { Name = "N", Description = "D", Active = true };
        var updated = new ClauseSelectDto { Id = 9, Name = "N" };
        _service.Setup(s => s.UpdateAsync(It.IsAny<ClauseUpdateDto>())).ReturnsAsync(updated);
        var res = await Create().Put(9, update);
        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task PutNotFoundWhenNull()
    {
        _service.Setup(s => s.UpdateAsync(It.IsAny<ClauseUpdateDto>())).ReturnsAsync((ClauseSelectDto?)null);
        var res = await Create().Put(9, new ClauseUpdateDto { Name = "X" });
        Assert.IsType<NotFoundResult>(res.Result);
    }

    [Fact]
    public async Task PostCreatedAt()
    {
        var created = new ClauseSelectDto { Id = 3, Name = "C" };
        _service.Setup(s => s.CreateAsync(It.IsAny<ClauseDto>())).ReturnsAsync(created);
        var res = await Create().Post(new ClauseDto { Name = "C", Description = "D" });
        Assert.IsType<CreatedAtActionResult>(res.Result);
    }

    [Fact]
    public async Task DeleteNoContentWhenDeleted()
    {
        _service.Setup(s => s.DeleteAsync(2)).ReturnsAsync(true);
        var res = await Create().Delete(2);
        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task DeleteNotFoundWhenMissing()
    {
        _service.Setup(s => s.DeleteAsync(22)).ReturnsAsync(false);
        var res = await Create().Delete(22);
        Assert.IsType<NotFoundResult>(res);
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
        var res = await Create().ChangeActiveStatus(
            7,
            new WebGESCOMPH.Contracts.Requests.ChangeActiveStatusRequest { Active = true }
        );
        Assert.IsType<NoContentResult>(res);
    }
}
