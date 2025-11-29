using Business.Services.Business;
using Business.Repository;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Microsoft.Extensions.Logging;
using Moq;

namespace Test.Modulo.Business;

public class PlazaServiceTests
{
    private readonly Mock<IPlazaRepository> _plazaRepo = new();
    private readonly Mock<IEstablishmentsRepository> _estRepo = new();
    private readonly Mock<IContractRepository> _contractRepo = new();
    private readonly Mock<ILogger<PlazaService>> _logger = new();
    private readonly PlazaService _service;

    public PlazaServiceTests()
    {
        _service = new PlazaService(
            _plazaRepo.Object,
            mapper: null!,
            _estRepo.Object,
            _contractRepo.Object,
            _logger.Object
        );
    }

    // ------------------------------------------------------------
    // Caso 1: Cambia de estado cuando es distinto
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateActiveStatusUpdatesWhenStateIsDifferent()
    {
        var entity = new Plaza { Id = 3, Active = false };
        _plazaRepo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(entity);

        await _service.UpdateActiveStatusAsync(3, true);

        _plazaRepo.Verify(
            r => r.UpdateAsync(It.Is<Plaza>(p => p.Id == 3 && p.Active == true)),
            Times.Once
        );
    }

    // ------------------------------------------------------------
    // Caso 2: Desactiva en cascada establecimientos
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateActiveStatusDisableCascadesToEstablishments()
    {
        var entity = new Plaza { Id = 5, Active = true };
        _plazaRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(entity);
        _contractRepo.Setup(r => r.AnyActiveByPlazaAsync(5)).ReturnsAsync(false);

        await _service.UpdateActiveStatusAsync(5, false);

        _plazaRepo.Verify(
            r => r.UpdateAsync(It.Is<Plaza>(p => p.Id == 5 && p.Active == false)),
            Times.Once
        );

        _estRepo.Verify(r => r.SetActiveByPlazaIdAsync(5, false), Times.Once);
    }

    // ------------------------------------------------------------
    // Caso 3: Bloquea desactivación si hay contratos activos
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateActiveStatusDisableThrowsWhenContractsAreActive()
    {
        var entity = new Plaza { Id = 7, Active = true };
        _plazaRepo.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(entity);
        _contractRepo.Setup(r => r.AnyActiveByPlazaAsync(7)).ReturnsAsync(true);

        await Assert.ThrowsAsync<Utilities.Exceptions.BusinessException>(
            () => _service.UpdateActiveStatusAsync(7, false)
        );

        _plazaRepo.Verify(r => r.UpdateAsync(It.IsAny<Plaza>()), Times.Never);
        _estRepo.Verify(r => r.SetActiveByPlazaIdAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    // ------------------------------------------------------------
    // Caso 4: No hace nada si el estado es el mismo
    // ------------------------------------------------------------
    [Fact]
    public async Task UpdateActiveStatusNoActionWhenStateIsSame()
    {
        var entity = new Plaza { Id = 8, Active = true };
        _plazaRepo.Setup(r => r.GetByIdAsync(8)).ReturnsAsync(entity);

        await _service.UpdateActiveStatusAsync(8, true);

        _plazaRepo.Verify(r => r.UpdateAsync(It.IsAny<Plaza>()), Times.Never);
        _estRepo.Verify(r => r.SetActiveByPlazaIdAsync(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }
}

