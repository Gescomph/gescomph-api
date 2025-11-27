using Business.Services.AdministrationSystem;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.AdministrationSystem.SystemParameter;
using MapsterMapper;
using Moq;
using Utilities.Exceptions;

namespace Test.Modulo.Business;

public class SystemParameterServiceTests
{
    private readonly Mock<IDataGeneric<SystemParameter>> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly SystemParameterService _service;

    public SystemParameterServiceTests()
    {
        _service = new SystemParameterService(_repo.Object, _mapper.Object);
    }

    [Fact]
    public async Task GetByIdThrowsWhenZero()
    {
        await Assert.ThrowsAsync<BusinessException>(() => _service.GetByIdAsync(0));
    }

    [Fact]
    public async Task DeleteWrapsDbUpdateException()
    {
        _repo.Setup(r => r.GetByIdAsync(1))
             .ReturnsAsync(new SystemParameter
             {
                 Id = 1,
                 Key = "K",
                 Value = "V",
                 Active = false
             });

        _repo.Setup(r => r.DeleteAsync(1))
             .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("fk"));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteAsync(1));

        Assert.Contains("restricciones de datos", ex.Message);
    }
}
