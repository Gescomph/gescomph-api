using Business.Services.Business;
using Data.Interfaz.IDataImplement.Business;
using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.DTOs.Implements.Business.EstablishmentDto;
using Entity.Infrastructure.Context;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities.Exceptions;

namespace Test.Modulo.Business
{
    public class EstablishmentServiceTests
    {
        private readonly Mock<IEstablishmentsRepository> _repo = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ILogger<EstablishmentService>> _logger = new();
        private readonly Mock<IDataGeneric<SystemParameter>> _systemParamRepo = new();
        private readonly ApplicationDbContext _ctx;
        private readonly EstablishmentService _service;

        public EstablishmentServiceTests()
        {
            var opt = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _ctx = new ApplicationDbContext(opt);

            _ctx.SystemParameters.Add(new SystemParameter
            {
                Id = 1,
                Key = "UVT",
                Value = "10",
                EffectiveFrom = DateTime.UtcNow.AddYears(-1),
                EffectiveTo = null,
                Active = true
            });

            _ctx.SaveChanges();

            _systemParamRepo
                .Setup(r => r.GetAllQueryable())
                .Returns(_ctx.SystemParameters.AsQueryable());

            _service = new EstablishmentService(
                _repo.Object,
                _ctx,
                _mapper.Object,
                _logger.Object,
                _systemParamRepo.Object
            );
        }

        // ------------------------------------------------------------
        // Payload inválido → Error
        // ------------------------------------------------------------
        [Fact]
        public async Task CreateThrowsWhenInvalidValues()
        {
            var dto = new EstablishmentCreateDto
            {
                AreaM2 = 0,
                UvtQty = 0,
                PlazaId = 0
            };

            await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(dto));
        }

        // ------------------------------------------------------------
        // Create correcto: cálculo UVT, persistencia y recarga
        // ------------------------------------------------------------
        [Fact]
        public async Task CreateSucceedsReturnsSelect()
        {
            var dto = new EstablishmentCreateDto
            {
                Name = "N",
                Description = "D",
                AreaM2 = 1,
                UvtQty = 2,
                PlazaId = 1
            };

            _repo.Setup(r => r.AddAsync(It.IsAny<Establishment>()))
                 .ReturnsAsync((Establishment e) =>
                 {
                     e.Id = 7;
                     return e;
                 });

            _repo.Setup(r => r.GetByIdAnyAsync(7))
                 .ReturnsAsync(new EstablishmentSelectDto
                 {
                     Id = 7,
                     Name = "N",
                     Description = "D",
                     AreaM2 = 1,
                     UvtQty = 2,
                     RentValueBase = 20,
                     PlazaId = 1
                 });

        _repo.Setup(r => r.AddAsync(It.IsAny<Establishment>()))
             .ReturnsAsync((Establishment e) => { e.Id = 7; return e; });

        _repo.Setup(r => r.GetByIdAnyAsync(7)).ReturnsAsync(entity);

            var result = await _service.CreateAsync(dto);

            Assert.Equal(7, result.Id);
            Assert.Equal(20, result.RentValueBase);
        }

        // ------------------------------------------------------------
        // Update: retorna null si no existe
        // ------------------------------------------------------------
        [Fact]
        public async Task UpdateReturnsNullWhenNotFound()
        {
            var dto = new EstablishmentUpdateDto
            {
                Id = 9,
                Name = "X",
                Description = "D",
                AreaM2 = 1,
                RentValueBase = 10,
                UvtQty = 1,
                PlazaId = 1
            };

            _repo.Setup(r => r.GetByIdAsync(9))
                 .ReturnsAsync((Establishment?)null);

            var result = await _service.UpdateAsync(dto);

            Assert.Null(result);
        }

        // ------------------------------------------------------------
        // Update correcto: recalcula UVT
        // ------------------------------------------------------------
        [Fact]
        public async Task UpdateSucceedsRecalculatesRentValue()
        {
            var dto = new EstablishmentUpdateDto
            {
                Id = 5,
                Name = "Updated",
                Description = "Desc",
                AreaM2 = 10,
                UvtQty = 3,
                PlazaId = 2
            };

            var existing = new Establishment
            {
                Id = 5,
                Name = "Old",
                Description = "OldDesc",
                AreaM2 = 5,
                UvtQty = 1,
                PlazaId = 2,
                RentValueBase = 10
            };

            _repo.Setup(r => r.GetByIdAsync(5))
                 .ReturnsAsync(existing);

            _repo.Setup(r => r.UpdateAsync(It.IsAny<Establishment>()))
                 .ReturnsAsync((Establishment e) => e);

            _repo.Setup(r => r.GetByIdAnyAsync(5))
                 .ReturnsAsync(new EstablishmentSelectDto
                 {
                     Id = 5,
                     Name = "Updated",
                     Description = "Desc",
                     AreaM2 = 10,
                     UvtQty = 3,
                     PlazaId = 2,
                     RentValueBase = 30
                 });

            var result = await _service.UpdateAsync(dto);

            Assert.NotNull(result);
            Assert.Equal(5, result!.Id);
            Assert.Equal(30, result.RentValueBase);
        }
    }
}
