using Business.Interfaces.Implements.Business;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.Business;

namespace Test.Modulo.Web;

public class ObligationMonthsControllerTests
{
    private readonly Mock<IObligationMonthService> _svc = new();
    private readonly Mock<IBackgroundJobClient> _jobs = new();
    private readonly Mock<IConfiguration> _cfg = new();
    private readonly Mock<ILogger<ObligationMonthsController>> _logger = new();

    private ObligationMonthsController Create() =>
        new(_svc.Object, _jobs.Object, _cfg.Object, _logger.Object);

    [Fact]
    public async Task GenerateBadRequestWhenInvalidMonth()
    {
        var res = await Create().Generate(2024, 13, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    [Fact]
    public void EnqueueBadRequestWhenInvalidMonth()
    {
        var res = Create().Enqueue(2024, 0);
        Assert.IsType<BadRequestObjectResult>(res);
    }

    // -----------------------------------------
    // Generic BaseController endpoints
    // -----------------------------------------

    [Fact]
    public async Task GetReturnsOk()
    {
        _svc.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<ObligationMonthSelectDto> { new() });

        var res = await Create().Get();

        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task GetByIdNotFound()
    {
        _svc.Setup(s => s.GetByIdAsync(9))
            .ReturnsAsync((ObligationMonthSelectDto?)null);

        var res = await Create().GetById(9);

        Assert.IsType<NotFoundResult>(res.Result);
    }

    [Fact]
    public async Task PostReturnsCreatedAt()
    {
        var created = new ObligationMonthSelectDto { Id = 2 };

        _svc.Setup(s => s.CreateAsync(It.IsAny<ObligationMonthDto>()))
            .ReturnsAsync(created);

        var res = await Create().Post(new ObligationMonthDto
        {
            ContractId = 1,
            Year = 2024,
            Month = 1
        });

        var result = Assert.IsType<CreatedAtActionResult>(res.Result);

        Assert.Equal("GetById", result.ActionName);
        Assert.Equal(201, result.StatusCode);
        Assert.Equal(created, result.Value);
    }

    [Fact]
    public async Task PutReturnsOk()
    {
        var updated = new ObligationMonthSelectDto { Id = 3 };

        _svc.Setup(s => s.UpdateAsync(It.IsAny<ObligationMonthUpdateDto>()))
            .ReturnsAsync(updated);

        var res = await Create().Put(3, new ObligationMonthUpdateDto { Id = 3 });

        Assert.IsType<OkObjectResult>(res.Result);
    }

    [Fact]
    public async Task DeleteReturnsNoContentWhenDeleted()
    {
        _svc.Setup(s => s.DeleteAsync(4)).ReturnsAsync(true);

        var res = await Create().Delete(4);

        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task DeleteLogicReturnsNoContentWhenDeleted()
    {
        _svc.Setup(s => s.DeleteLogicAsync(5)).ReturnsAsync(true);

        var res = await Create().DeleteLogic(5);

        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task ChangeActiveStatusReturnsNoContent()
    {
        var res = await Create().ChangeActiveStatus(
            6,
            new WebGESCOMPH.Contracts.Requests.ChangeActiveStatusRequest { Active = true }
        );

        Assert.IsType<NoContentResult>(res);
    }
}
