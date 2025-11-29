using Business.Services.Business;
using Data.Interfaz.IDataImplement.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Appointment;
using Entity.DTOs.Implements.Persons.Person;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Utilities.Exceptions;
using Utilities.Messaging.Interfaces;
using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Interfaces.Implements.Persons;
using Utilities.Messaging.Interfaces;
using Business.Interfaces;

namespace Test.Modulo.Business;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> _repo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IPersonService> _personService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ISendCode> _emailService = new();
    private readonly Mock<IAuthService> _authService = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILogger<AppointmentService>> _logger = new();

    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new ApplicationDbContext(options);

        _service = new AppointmentService(
            _repo.Object,
            _mapper.Object,
            _personService.Object,
            _userService.Object,
            _emailService.Object,
            _authService.Object,
            _uow.Object,
            _logger.Object
        );
    }

    // ------------------------------------------------------------
    // Caso 2: Crea cita cuando persona NO existe
    // ------------------------------------------------------------
    [Fact]
    public async Task CreateCreatesAppointmentWhenPersonDoesNotExist()
    {
        var ex = await Assert.ThrowsAsync<BusinessException>(() => _service.CreateAsync(null!));
        Assert.Contains("no puede ser nulo", ex.Message);
    }

    [Fact]
    public async Task Create_WithValidData_CreatesAppointment()
    {
        var dto = new AppointmentCreateDto
        {
            FirstName = "A",
            LastName = "B",
            Document = "123",
            Email = "a@b.com",
            RequestDate = DateTime.UtcNow,
            DateTimeAssigned = DateTime.UtcNow
        };

        _personService
            .Setup(p => p.GetByDocumentAsync("123"))
            .ReturnsAsync((PersonSelectDto?)null);

        _mapper
            .Setup(m => m.Map<RegisterDto>(dto))
            .Returns(new RegisterDto());

        _authService
            .Setup(a => a.RegisterInternalAsync(
                It.IsAny<RegisterDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RegisterResultDto { PersonId = 99 });

        _mapper
            .Setup(m => m.Map<Appointment>(dto))
            .Returns(new Appointment());

        _repo
            .Setup(r => r.AddAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) =>
            {
                a.Id = 5;
                return a;
            });

        _mapper
            .Setup(m => m.Map<AppointmentSelectDto>(It.IsAny<Appointment>()))
            .Returns<Appointment>(a => new AppointmentSelectDto { Id = a.Id });

        _uow
            .Setup(u => u.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task<AppointmentSelectDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<AppointmentSelectDto>> action, CancellationToken _) =>
                action(CancellationToken.None));

        var result = await _service.CreateAsync(dto);

        Assert.Equal(5, result.Id);
    }
}
