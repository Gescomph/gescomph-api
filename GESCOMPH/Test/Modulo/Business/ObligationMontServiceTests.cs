using Business.Interfaces.Implements.Business;
using Business.Services.Business;
using Data.Interfaz.DataBasic;
using Data.Interfaz.IDataImplement.Business;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using MapsterMapper;
using Moq;

namespace Test.Modulo.Business;

public class ObligationMonthServiceTests
{
    private readonly Mock<IObligationMonthRepository> _obligationRepo = new();
    private readonly Mock<IContractRepository> _contractRepo = new();
    private readonly Mock<IDataGeneric<SystemParameter>> _systemParamRepo = new();
    private readonly Mock<IObligationNotifier> _notifier = new();
    private readonly Mock<IMapper> _mapper = new();

    private readonly ObligationMonthService _service;

    public ObligationMonthServiceTests()
    {
        _service = new ObligationMonthService(
            _obligationRepo.Object,
            _contractRepo.Object,
            _systemParamRepo.Object,
            _notifier.Object,
            _mapper.Object
        );
    }

    // ---------------------------------------------------------
    // TOTAL PAGADO POR DÍA
    // ---------------------------------------------------------
    [Fact]
    public async Task GetTotalObligationsPaidByDayAsyncReturnsExpected()
    {
        // Arrange
        var date = new DateTime(2024, 10, 1);

        _obligationRepo.Setup(r => r.GetTotalObligationsPaidByDayAsync(date))
                       .ReturnsAsync(1500m);

        // Act
        var result = await _service.GetTotalObligationsPaidByDayAsync(date);

        // Assert
        Assert.Equal(1500m, result);
        _obligationRepo.Verify(r => r.GetTotalObligationsPaidByDayAsync(date), Times.Once);
    }

    // ---------------------------------------------------------
    // TOTAL PAGADO POR MES
    // ---------------------------------------------------------
    [Fact]
    public async Task GetTotalObligationsPaidByMonthAsyncReturnsExpected()
    {
        // Arrange
        int year = 2024, month = 10;

        _obligationRepo.Setup(r => r.GetTotalObligationsPaidByMonthAsync(year, month))
                       .ReturnsAsync(5000m);

        // Act
        var result = await _service.GetTotalObligationsPaidByMonthAsync(year, month);

        // Assert
        Assert.Equal(5000m, result);
        _obligationRepo.Verify(r => r.GetTotalObligationsPaidByMonthAsync(year, month), Times.Once);
    }
}
