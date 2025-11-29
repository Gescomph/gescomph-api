using Business.Interfaces.Implements.Business;
using Business.Interfaces.PDF;
using Entity.DTOs.Implements.Business.Contract;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.Business;
using WebGESCOMPH.RealTime;
using WebGESCOMPH.RealTime.Contract;
using Business.Interfaces.Notifications;

namespace Test.Modulo.Web;

public class ContractControllerTests
{
    private readonly Mock<IContractService> _service = new();
    private readonly Mock<IContractPdfGeneratorService> _pdf = new();
    private readonly Mock<ILogger<ContractController>> _logger = new();
    private readonly Mock<IHubContext<ContractsHub>> _hub = new();
    private readonly Mock<IContractNotificationService> _notify = new();

    private ContractController Create()
        => new(_service.Object, _pdf.Object, _logger.Object, _hub.Object, _notify.Object);

    // ----------------------------------------------------------
    // GetMine
    // ----------------------------------------------------------
    [Fact]
    public async Task GetMineReturnsOk()
    {
        _service.Setup(s => s.GetMineAsync()).ReturnsAsync(new List<ContractSelectDto>());
        var res = await Create().GetMine();
        Assert.IsType<OkObjectResult>(res);
    }

    // ----------------------------------------------------------
    // GET PDF not found
    // ----------------------------------------------------------
    [Fact]
    public async Task DownloadContractPdfNotFoundWhenMissing()
    {
        _service.Setup(s => s.GetByIdAsync(77)).ReturnsAsync((ContractSelectDto?)null);
        var res = await Create().DownloadContractPdf(77);
        Assert.IsType<NotFoundObjectResult>(res);
    }

    // ----------------------------------------------------------
    // POST: Create returns OK (NOT 201)
    // ----------------------------------------------------------
    [Fact]
    public async Task PostReturnsOkWithResult()
    {
        var dto = new ContractCreateDto
        {
            FirstName = "A",
            LastName = "B",
            Document = "1",
            Phone = "P",
            Address = "Addr",
            CityId = 1,
            EstablishmentIds = new() { 1 }
        };

        var expected = new ContractSelectDto { PersonId = 7 };

        _service.Setup(s => s.CreateAsync(dto)).ReturnsAsync(expected);

        var res = await Create().Post(dto);

        var ok = Assert.IsType<OkObjectResult>(res.Result);

        Assert.Equal(expected, ok.Value);
    }

    // ----------------------------------------------------------
    // ChangeActiveStatus returns 204
    // ----------------------------------------------------------
    [Fact]
    public async Task ChangeActiveStatusNoContent()
    {
        _service.Setup(s => s.UpdateActiveStatusAsync(5, true))
                .Returns(Task.CompletedTask);

        _service.Setup(s => s.GetByIdAsync(5))
                .ReturnsAsync(new ContractSelectDto { PersonId = 7 });

        _notify.Setup(n => n.NotifyContractStatusChanged(5, true, 7))
               .Returns(Task.CompletedTask);

        var res = await Create().ChangeActiveStatus(5, new WebGESCOMPH.Contracts.Requests.ChangeActiveStatusRequest { Active = true });
        Assert.IsType<NoContentResult>(res);
    }

    // ----------------------------------------------------------
    // Delete returns 204
    // ----------------------------------------------------------
    [Fact]
    public async Task DeleteNoContent()
    {
        _service.Setup(s => s.GetByIdAsync(3))
                .ReturnsAsync(new ContractSelectDto { PersonId = 9 });

        _service.Setup(s => s.DeleteAsync(3)).ReturnsAsync(true);

        _notify.Setup(n => n.NotifyContractDeleted(3, 9))
               .Returns(Task.CompletedTask);

        var res = await Create().Delete(3);
        Assert.IsType<NoContentResult>(res);
    }

    // ----------------------------------------------------------
    // GetObligations NotFound
    // ----------------------------------------------------------
    [Fact]
    public async Task GetObligationsNotFoundWhenContractMissing()
    {
        _service.Setup(s => s.GetByIdAsync(9)).ReturnsAsync((ContractSelectDto?)null);

        var res = await Create().GetObligations(9);
        Assert.IsType<NotFoundResult>(res);
    }

    // ----------------------------------------------------------
    // GetObligations OK
    // ----------------------------------------------------------
    [Fact]
    public async Task GetObligationsOkWhenExists()
    {
        _service.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new ContractSelectDto());

        _service.Setup(s => s.GetObligationsAsync(1))
                .ReturnsAsync(new List<Entity.DTOs.Implements.Business.ObligationMonth.ObligationMonthSelectDto> { new() });

        var res = await Create().GetObligations(1);
        Assert.IsType<OkObjectResult>(res);
    }

    // ----------------------------------------------------------
    // PDF OK
    // ----------------------------------------------------------
    [Fact]
    public async Task DownloadContractPdfOkReturnsPdf()
    {
        _service.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(new ContractSelectDto { FullName = "X" });

        _pdf.Setup(p => p.GeneratePdfAsync(It.IsAny<ContractSelectDto>()))
            .ReturnsAsync(new byte[] { 1, 2, 3 });

        var res = await Create().DownloadContractPdf(1);

        var file = Assert.IsType<FileContentResult>(res);

        Assert.Equal("application/pdf", file.ContentType);
        Assert.NotEmpty(file.FileContents);
    }
}
