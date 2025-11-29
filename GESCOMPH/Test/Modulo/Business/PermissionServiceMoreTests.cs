using Business.Services.SecurityAuthentication;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Permission;
using MapsterMapper;
using Moq;
using Utilities.Exceptions;

namespace Test.Modulo.Business;

public class PermissionServiceMoreTests
{
    private readonly Mock<IDataGeneric<Permission>> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly PermissionService _service;

    public PermissionServiceMoreTests()
    {
        _service = new PermissionService(_repo.Object, _mapper.Object);
    }

    [Fact]
    public async Task DeleteThrowsWhenIdZero()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteAsync(0));
        Assert.Contains("mayor que cero", ex.InnerException!.Message);
    }

    [Fact]
    public async Task DeleteWrapsDbUpdateException()
    {
        _repo.Setup(r => r.GetByIdAsync(1))
             .ReturnsAsync(new Permission
             {
                 Id = 1,
                 Name = "Perm1",
                 Description = "Test",
                 Active = false
             });

        _repo.Setup(r => r.DeleteAsync(1))
             .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("FK"));

        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteAsync(1));

        Assert.Contains("restricciones de datos", ex.Message);
    }

    [Fact]
    public async Task DeleteLogicThrowsWhenIdZero()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.DeleteLogicAsync(0));
        Assert.Contains("mayor que cero", ex.InnerException!.Message);
    }

    [Fact]
    public async Task UpdateActiveStatusUpdatesWhenFound()
    {
        var entity = new Permission { Id = 3, Name = "P", Description = "D", Active = false };

        _repo.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(entity);

        await _service.UpdateActiveStatusAsync(3, true);

        _repo.Verify(r => r.UpdateAsync(It.Is<Permission>(m => m.Id == 3 && m.Active == true)), Times.Once);
    }
}

