using Business.Services.SecurityAuthentication;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Rol;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using Utilities.Exceptions;

namespace Test.Modulo.RolRolTest
{
    public class RolServiceTests
    {
        private readonly Mock<IDataGeneric<Rol>> _rolRepoMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly RolService _rolService;

        public RolServiceTests()
        {
            _rolRepoMock = new Mock<IDataGeneric<Rol>>();
            _mapperMock = new Mock<IMapper>();
            _rolService = new RolService(_rolRepoMock.Object, _mapperMock.Object);
        }

        // ---------- GETALL ----------
        [Fact]
        public async Task GetAllAsync()
        {
            var roles = new List<Rol>
            {
                new Rol { Id = 1, Name = "Admin" },
                new Rol { Id = 2, Name = "User" }
            };

            _rolRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(roles);
            _mapperMock.Setup(m => m.Map<IEnumerable<RolSelectDto>>(roles))
                       .Returns(new List<RolSelectDto>
                       {
                           new RolSelectDto { Id = 1, Name = "Admin" },
                           new RolSelectDto { Id = 2, Name = "User" }
                       });

            var result = await _rolService.GetAllAsync();

            Assert.NotNull(result);
            Assert.Collection(result,
                r => Assert.Equal("Admin", r.Name),
                r => Assert.Equal("User", r.Name));
        }

        [Fact]
        public async Task GetAllAsync_WhenRepoFails_ThrowsBusinessException()
        {
            _rolRepoMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new Exception("DB failure"));

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.GetAllAsync());

            Assert.Contains("Error al obtener todos los registros", ex.Message);
        }

        // ---------- GETBYID ----------
        [Fact]
        public async Task GetByIdAsync()
        {
            var rol = new Rol { Id = 1, Name = "Admin" };

            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(rol);
            _mapperMock.Setup(m => m.Map<RolSelectDto>(rol))
                       .Returns(new RolSelectDto { Id = 1, Name = "Admin" });

            var result = await _rolService.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Admin", result!.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WhenZero_ThrowsBusinessException()
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.GetByIdAsync(0));

            Assert.Contains("Error al obtener el registro con ID 0", ex.Message);
        }

        [Fact]
        public async Task GetByIdAsync_WhenNull_ReturnsNull()
        {
            _rolRepoMock.Setup(r => r.GetByIdAsync(10)).ReturnsAsync((Rol?)null);

            var result = await _rolService.GetByIdAsync(10);

            Assert.Null(result);
        }

        // ---------- CREATE ----------
        [Fact]
        public async Task CreateAsync()
        {
            var dto = new RolCreateDto { Name = "Admin" };
            var entity = new Rol { Name = "Admin" };

            _mapperMock.Setup(m => m.Map<Rol>(dto)).Returns(entity);
            _rolRepoMock.Setup(r => r.GetAllQueryable()).Returns(new List<Rol>().AsQueryable());
            _rolRepoMock.Setup(r => r.AddAsync(It.IsAny<Rol>()))
                        .ReturnsAsync((Rol r) => { r.Id = 1; return r; });

            await _rolService.CreateAsync(dto);

            _rolRepoMock.Verify(r => r.AddAsync(It.IsAny<Rol>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_WhenDuplicateActive_ThrowsBusinessException()
        {
            var dto = new RolCreateDto { Name = "Admin" };
            var entity = new Rol { Name = "Admin" };

            _mapperMock.Setup(m => m.Map<Rol>(dto)).Returns(entity);

            _rolRepoMock.Setup(r => r.GetAllQueryable())
                .Returns(new List<Rol>
                {
                    new Rol { Id = 1, Name = "Admin", IsDeleted = false }
                }.AsQueryable());

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.CreateAsync(dto));

            Assert.Equal("Duplicado", ex.Message);
        }

        [Fact]
        public async Task CreateAsync_WhenDuplicateInactive_Reactivates()
        {
            var dto = new RolCreateDto { Name = "Admin" };
            var entity = new Rol { Name = "Admin" };

            _mapperMock.Setup(m => m.Map<Rol>(dto)).Returns(entity);

            var existing = new Rol { Id = 1, Name = "Admin", IsDeleted = true };

            _rolRepoMock.Setup(r => r.GetAllQueryable())
                .Returns(new List<Rol> { existing }.AsQueryable());

            _rolRepoMock.Setup(r => r.UpdateAsync(existing))
                .ReturnsAsync(existing);

            await _rolService.CreateAsync(dto);

            _rolRepoMock.Verify(r => r.UpdateAsync(It.Is<Rol>(x => x.Id == 1 && x.IsDeleted == false)), Times.Once);
        }


        [Fact]
        public async Task CreateAsync_WhenDbUpdateException_Propagates()
        {
            var dto = new RolCreateDto { Name = "Admin" };
            var entity = new Rol { Name = "Admin" };

            _mapperMock.Setup(m => m.Map<Rol>(dto)).Returns(entity);
            _rolRepoMock.Setup(r => r.GetAllQueryable()).Returns(new List<Rol>().AsQueryable());

            _rolRepoMock.Setup(r => r.AddAsync(It.IsAny<Rol>()))
                        .ThrowsAsync(new DbUpdateException("Unique"));

            await Assert.ThrowsAsync<DbUpdateException>(() => _rolService.CreateAsync(dto));
        }

        [Fact]
        public async Task CreateAsync_WhenGenericException_Propagates()
        {
            var dto = new RolCreateDto { Name = "Admin" };
            var entity = new Rol { Name = "Admin" };

            _mapperMock.Setup(m => m.Map<Rol>(dto)).Returns(entity);
            _rolRepoMock.Setup(r => r.GetAllQueryable()).Returns(new List<Rol>().AsQueryable());

            _rolRepoMock.Setup(r => r.AddAsync(It.IsAny<Rol>()))
                        .ThrowsAsync(new Exception("X"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _rolService.CreateAsync(dto));

            Assert.Equal("X", ex.Message);
        }

        // ---------- UPDATE ----------
        [Fact]
        public async Task UpdateAsync()
        {
            var dto = new RolUpdateDto { Id = 1, Name = "Updated" };
            var entity = new Rol { Id = 1, Name = "Updated" };

            _mapperMock.Setup(m => m.Map<Rol>(dto)).Returns(entity);
            _rolRepoMock.Setup(r => r.UpdateAsync(entity)).ReturnsAsync(entity);

            await _rolService.UpdateAsync(dto);

            _rolRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Rol>()), Times.Once);
        }

        // ---------- DELETE ----------
        [Fact]
        public async Task DeleteAsync()
        {
            var entity = new Rol { Id = 1, Active = false };
            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
            _rolRepoMock.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            await _rolService.DeleteAsync(1);

            _rolRepoMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenIdZero_ThrowsBusinessException()
        {
            await Assert.ThrowsAsync<BusinessException>(() => _rolService.DeleteAsync(0));
        }

        [Fact]
        public async Task DeleteAsync_WhenDbUpdateException_Wrapped()
        {
            var entity = new Rol { Id = 1, Active = false };
            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            _rolRepoMock.Setup(r => r.DeleteAsync(1))
                        .ThrowsAsync(new DbUpdateException("FK"));

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.DeleteAsync(1));

            Assert.Contains("restricciones de datos", ex.Message);
        }

        [Fact]
        public async Task DeleteAsync_WhenGenericException_Wrapped()
        {
            var entity = new Rol { Id = 1, Active = false };
            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            _rolRepoMock.Setup(r => r.DeleteAsync(1))
                        .ThrowsAsync(new Exception("X"));

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.DeleteAsync(1));

            Assert.Contains("Error al eliminar el registro", ex.Message);
            Assert.Equal("X", ex.InnerException!.Message);
        }

        // ---------- DELETE LOGIC ----------
        [Fact]
        public async Task DeleteLogicAsync()
        {
            var entity = new Rol { Id = 1, Active = false };
            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
            _rolRepoMock.Setup(r => r.DeleteLogicAsync(1)).ReturnsAsync(true);

            await _rolService.DeleteLogicAsync(1);

            _rolRepoMock.Verify(r => r.DeleteLogicAsync(1), Times.Once);
        }

        [Fact]
        public async Task DeleteLogicAsync_WhenIdZero_ThrowsBusinessException()
        {
            await Assert.ThrowsAsync<BusinessException>(() => _rolService.DeleteLogicAsync(0));
        }

        [Fact]
        public async Task DeleteLogicAsync_WhenGenericError_Wrapped()
        {
            var entity = new Rol { Id = 1, Active = false };
            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            _rolRepoMock.Setup(r => r.DeleteLogicAsync(1))
                        .ThrowsAsync(new Exception("Delete logic fail"));

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.DeleteLogicAsync(1));

            Assert.Contains("Error al eliminar lógicamente", ex.Message);
        }

        // ---------- UPDATE ACTIVE STATUS ----------
        [Fact]
        public async Task UpdateActiveStatusAsync()
        {
            var entity = new Rol { Id = 1, Active = false };

            _rolRepoMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

            await _rolService.UpdateActiveStatusAsync(1, true);

            _rolRepoMock.Verify(r => r.UpdateAsync(It.Is<Rol>(x => x.Id == 1 && x.Active == true)), Times.Once);
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_WhenIdZero_ThrowsBusinessException()
        {
            await Assert.ThrowsAsync<BusinessException>(() => _rolService.UpdateActiveStatusAsync(0, true));
        }

        [Fact]
        public async Task UpdateActiveStatusAsync_WhenNotFound_ThrowsBusinessException()
        {
            _rolRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Rol?)null);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _rolService.UpdateActiveStatusAsync(99, true));

            Assert.Contains("Error al actualizar el estado del registro con ID 99", ex.Message);
            Assert.IsType<KeyNotFoundException>(ex.InnerException);
        }
    }
}
