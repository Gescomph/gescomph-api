using Business.CustomJWT;
using Business.Interfaces;
using Business.Interfaces.Implements.AdministrationSystem;
using Business.Interfaces.Implements.Business;
using Business.Interfaces.Implements.Persons;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Interfaces.Notifications;
using Business.Interfaces.PDF;
using Business.Services.Business;
using Data.Interfaz.IDataImplement.AdministrationSystem;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Contract;
using Entity.DTOs.Implements.Business.ObligationMonth;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities.Exceptions;
using Utilities.Messaging.Interfaces;

namespace Test.Modulo.Business
{
    public class ContractServiceTests
    {
        private readonly Mock<IContractRepository> _contracts = new();
        private readonly Mock<IObligationMonthService> _obligationSvc = new();
        private readonly Mock<IPersonService> _personSvc = new();
        private readonly Mock<IEstablishmentService> _estSvc = new();
        private readonly Mock<IAuthService> _authService = new();
        private readonly Mock<ISendCode> _email = new();
        private readonly Mock<ICurrentUser> _currentUser = new();
        private readonly Mock<IUserContextService> _userCtx = new();
        private readonly Mock<IContractPdfGeneratorService> _contractPdfService = new();
        private readonly Mock<IContractNotificationService> _contractNotificationService = new();
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly Mock<INotificationRepository> _notificationRepository = new();
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<ILogger<ContractService>> _logger = new();
        private readonly Mock<IMapper> _mapper = new();

        private readonly ContractService _service;

        public ContractServiceTests()
        {
            _service = new ContractService(
                _contracts.Object,
                _personSvc.Object,
                _estSvc.Object,
                _authService.Object,
                _email.Object,
                _currentUser.Object,
                _obligationSvc.Object,
                _userCtx.Object,
                _contractPdfService.Object,
                _contractNotificationService.Object,
                _notificationService.Object,
                _notificationRepository.Object,
                _uow.Object,
                _logger.Object,
                _mapper.Object);
        }

        // ------------------------------------------------------------
        // Caso 1: Admin → obtiene todos los contratos
        // ------------------------------------------------------------
        [Fact]
        public async Task GetMineAdminReturnsMappedContracts()
        {
            _currentUser.SetupGet(u => u.EsAdministrador).Returns(true);

            var entities = new List<Contract>
            {
                new Contract { Id = 1, PersonId = 10 }
            };

            _contracts.Setup(r => r.GetAllAsync())
                .ReturnsAsync(entities);

            _mapper.Setup(m => m.Map<IEnumerable<ContractSelectDto>>(entities))
                .Returns(new List<ContractSelectDto>
                {
                    new ContractSelectDto { Id = 1, PersonId = 10 }
                });

            var result = await _service.GetMineAsync();

            Assert.Single(result);
            Assert.Equal(10, result.First().PersonId);
        }

        // ------------------------------------------------------------
        // Caso 2: No admin y sin persona → lanza excepción
        // ------------------------------------------------------------
        [Fact]
        public async Task GetMineNonAdminWithoutPersonThrows()
        {
            _currentUser.SetupGet(u => u.EsAdministrador).Returns(false);
            _currentUser.SetupGet(u => u.PersonId).Returns((int?)null);

            var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.GetMineAsync());

            Assert.Contains("persona asociada", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------
        // Caso 3: No admin con persona → retorna contratos filtrados
        // ------------------------------------------------------------
        [Fact]
        public async Task GetMineNonAdminWithPersonReturnsMappedContracts()
        {
            _currentUser.SetupGet(u => u.EsAdministrador).Returns(false);
            _currentUser.SetupGet(u => u.PersonId).Returns(99);

            var entities = new List<Contract>
            {
                new Contract { Id = 2, PersonId = 99 }
            };

            _contracts.Setup(r => r.GetByPersonAsync(99))
                .ReturnsAsync(entities);

            _mapper.Setup(m => m.Map<IEnumerable<ContractSelectDto>>(entities))
                .Returns(new List<ContractSelectDto>
                {
                    new ContractSelectDto { Id = 2, PersonId = 99 }
                });

            var result = await _service.GetMineAsync();

            Assert.Single(result);
            Assert.Equal(99, result.First().PersonId);
        }

        // ------------------------------------------------------------
        // Caso 4: Id inválido en GetObligationsAsync
        // ------------------------------------------------------------
        [Fact]
        public async Task GetObligationsInvalidIdThrows()
        {
            var ex = await Assert.ThrowsAsync<BusinessException>(() =>
                _service.GetObligationsAsync(0));

            Assert.Contains("inválido", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ------------------------------------------------------------
        // Caso 5: Delegación correcta hacia el servicio de obligaciones
        // ------------------------------------------------------------
        [Fact]
        public async Task GetObligationsDelegatesToService()
        {
            _obligationSvc.Setup(s => s.GetByContractAsync(5))
                .ReturnsAsync(new List<ObligationMonthSelectDto> { new() });

            var result = await _service.GetObligationsAsync(5);

            Assert.Single(result);
            _obligationSvc.Verify(s => s.GetByContractAsync(5), Times.Once);
        }
    }
}
