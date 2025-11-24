using Business.Interfaces.Implements.Business;
using Business.Services.Business.Payments;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using Entity.DTOs.Implements.Payments;
using Entity.Domain.Models.Implements.Business;
using Entity.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.Business;
using Xunit;

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

    [Fact]
    public async Task CreateCheckout_ReturnsBadRequest_WhenObligationIsPaid()
    {
        // Arrange
        int contractId = 1;
        int obligationId = 10;
        var contract = new ContractSelectDto { Id = contractId, Email = "test@test.com", Document = "123" };
        var obligation = new ObligationMonthSelectDto 
        { 
            Id = obligationId, 
            ContractId = contractId, 
            Status = Status.Aprobada, // Already paid
            Locked = true 
        };

        _contractService.Setup(s => s.GetByIdAsync(contractId)).ReturnsAsync(contract);
        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);

        // Act
        var result = await Create().CreateCheckout(contractId, obligationId);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ya ha sido pagada", badRequest.Value.ToString());
    }

    [Fact]
    public async Task CreateCheckout_ReturnsOk_WhenObligationIsPending()
    {
        // Arrange
        int contractId = 1;
        int obligationId = 10;
        var contract = new ContractSelectDto { Id = contractId, Email = "test@test.com", Document = "123" };
        var obligation = new ObligationMonthSelectDto 
        { 
            Id = obligationId, 
            ContractId = contractId, 
            Status = Status.Pendiente, 
            Locked = false,
            TotalAmount = 10000
        };

        _contractService.Setup(s => s.GetByIdAsync(contractId)).ReturnsAsync(contract);
        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);
        
        _mercadoPagoService.Setup(s => s.CreateCheckoutPreferenceAsync(
            It.IsAny<ObligationMonthSelectDto>(),
            It.IsAny<ContractSelectDto>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(new MercadoPagoPreferenceResult { InitPoint = "https://mercadopago.com/checkout" });

        // Act
        var result = await Create().CreateCheckout(contractId, obligationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        // We can check the value if needed, but type check is good for now
    }

    [Fact]
    public async Task CheckoutObligation_ReturnsBadRequest_WhenObligationIsPaid()
    {
        // Arrange
        int obligationId = 20;
        var obligation = new ObligationMonthSelectDto 
        { 
            Id = obligationId, 
            ContractId = 2, 
            Status = Status.Aprobada, 
            Locked = true 
        };

        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);

        // Act
        var result = await Create().CheckoutObligation(obligationId);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("ya ha sido pagada", badRequest.Value.ToString());
    }

    [Fact]
    public async Task CheckoutObligation_ReturnsOk_WhenObligationPending()
    {
        // Arrange
        int obligationId = 30;
        var obligation = new ObligationMonthSelectDto
        {
            Id = obligationId,
            ContractId = 3,
            Status = Status.Pendiente,
            Locked = false,
            TotalAmount = 5000
        };

        var contract = new ContractSelectDto { Id = 3, Email = "user@test.com", Document = "456" };

        _obligationService.Setup(s => s.GetByIdAsync(obligationId)).ReturnsAsync(obligation);
        _contractService.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(contract);
        _mercadoPagoService.Setup(s => s.CreateCheckoutPreferenceAsync(
            It.IsAny<ObligationMonthSelectDto>(),
            It.IsAny<ContractSelectDto>(),
            It.IsAny<string>(),
            It.IsAny<string>()
        )).ReturnsAsync(new MercadoPagoPreferenceResult { InitPoint = "https://mercadopago.com/checkout" });

        // Act
        var result = await Create().CheckoutObligation(obligationId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
    }
}
