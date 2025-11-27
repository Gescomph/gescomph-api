using Business.Interfaces.Implements.Business;
using Business.Services.Business.Payments;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Payments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.Business;

namespace Test.Modulo.Web;

public class PaymentsControllerTests
{
    private readonly Mock<IContractService> _contractService = new();
    private readonly Mock<IObligationMonthService> _obligationService = new();
    private readonly Mock<IMercadoPagoService> _mercadoPagoService = new();
    private readonly Mock<ILogger<PaymentsController>> _logger = new();

    private PaymentsController Create() => new(
        _contractService.Object,
        _obligationService.Object,
        _mercadoPagoService.Object,
        _logger.Object
    );

    // =====================================================================
    // CreateCheckout
    // =====================================================================
    [Fact]
    public async Task CreateCheckoutReturnsBadRequestWhenObligationIsPaid()
    {
        int contractId = 1;
        int obligationId = 10;

        var contract = new ContractSelectDto
        {
            Id = contractId,
            Email = "test@test.com",
            Document = "123"
        };

        var obligation = new ObligationMonthSelectDto
        {
            Id = obligationId,
            ContractId = contractId,
            Status = "Aprobada",
            Locked = true
        };

        _contractService.Setup(s => s.GetByIdAsync(contractId)).ReturnsAsync(contract);
        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);

        var result = await Create().CreateCheckout(contractId, obligationId);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ya ha sido pagada", bad.Value!.ToString());
    }

    [Fact]
    public async Task CreateCheckoutReturnsOkWhenObligationIsPending()
    {
        int contractId = 1;
        int obligationId = 10;

        var contract = new ContractSelectDto
        {
            Id = contractId,
            Email = "test@test.com",
            Document = "123"
        };

        var obligation = new ObligationMonthSelectDto
        {
            Id = obligationId,
            ContractId = contractId,
            Status = "Pendiente",
            Locked = false
        };

        _contractService.Setup(s => s.GetByIdAsync(contractId)).ReturnsAsync(contract);
        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);

        _mercadoPagoService
            .Setup(s => s.CreateCheckoutPreferenceAsync(
                It.IsAny<ObligationMonthSelectDto>(),
                It.IsAny<ContractSelectDto>(),
                It.IsAny<string>(),
                It.IsAny<string>()
            ))
            .ReturnsAsync(new MercadoPagoPreferenceResult
            {
                InitPoint = "https://mp.com/checkout",
                PreferenceId = "pref-001",
                ObligationId = obligationId,
                ContractId = contractId,
                Amount = 10000
            });

        var result = await Create().CreateCheckout(contractId, obligationId);

        Assert.IsType<OkObjectResult>(result);
    }

    // =====================================================================
    // CheckoutObligation
    // =====================================================================
    [Fact]
    public async Task CheckoutObligationReturnsBadRequestWhenPaid()
    {
        int obligationId = 20;

        var obligation = new ObligationMonthSelectDto
        {
            Id = obligationId,
            ContractId = 2,
            Status = "Aprobada",
            Locked = true
        };

        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);

        var result = await Create().CheckoutObligation(obligationId);

        var bad = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ya ha sido pagada", bad.Value!.ToString());
    }

    [Fact]
    public async Task CheckoutObligationReturnsOkWhenPending()
    {
        int obligationId = 30;

        var obligation = new ObligationMonthSelectDto
        {
            Id = obligationId,
            ContractId = 3,
            Status = "Pendiete",
            Locked = false
        };

        var contract = new ContractSelectDto
        {
            Id = 3,
            Email = "user@test.com",
            Document = "456"
        };

        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);
        _contractService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(contract);

        _mercadoPagoService.Setup(s => s.CreateCheckoutPreferenceAsync(
            It.IsAny<ObligationMonthSelectDto>(),
            It.IsAny<ContractSelectDto>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(new MercadoPagoPreferenceResult
        {
            InitPoint = "https://mp.com/checkout",
            PreferenceId = "pref-XYZ",
            ObligationId = obligationId,
            ContractId = 3,
            Amount = 5000
        });

        var result = await Create().CheckoutObligation(obligationId);

        Assert.IsType<OkObjectResult>(result);
    }
}
