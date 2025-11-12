using Business.Interfaces.Implements.SecurityAuthentication;
using Business.Repository;
using Data.Interfaz.IDataImplement.Persons;
using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using MapsterMapper;
using Microsoft.AspNetCore.Identity;
using System.Linq.Expressions;
using Utilities.Exceptions;

namespace Business.Services.SecurityAuthentication
{
    /// <summary>
    /// Servicio de dominio para User.
    /// - Se limita a reglas y operaciones propias del agregado User.
    /// - NO crea/edita Person, NO asigna roles, NO envía correos, NO genera contraseñas temporales.
    /// - El hashing de contraseña se realiza SOLO cuando el caso de uso lo pide explícitamente.
    /// </summary>
    public class UserService
        : BusinessGeneric<UserSelectDto, UserCreateDto, UserUpdateDto, User>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly IPersonRepository _personRepository; // opcional: solo para validar existencia

        public UserService(
            IUserRepository userRepository,
            IMapper mapper,
            IPasswordHasher<User> passwordHasher,
            IPersonRepository personRepository // si no quieres validar existencia, puedes quitarlo
        ) : base(userRepository, mapper)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _personRepository = personRepository;
        }

        // ======================================================
        // LECTURAS
        // ======================================================

        public override async Task<IEnumerable<UserSelectDto>> GetAllAsync()
        {
            // Lectura directa desde el repo (puede traer Person/RolUsers, pero este servicio NO compone Roles).
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserSelectDto>>(users);
        }

        public override async Task<UserSelectDto?> GetByIdAsync(int id)
        {
            var entity = await _userRepository.GetByIdAsync(id);
            return _mapper.Map<UserSelectDto?>(entity);
        }

        // ======================================================
        // CREAR (solo User; requiere PersonId válido y Password provista)
        // ======================================================

        public override async Task<UserSelectDto> CreateAsync(UserCreateDto dto)
        {
            if (dto is null) throw new BusinessException("Datos requeridos.");

            // Validaciones básicas
            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new BusinessException("El correo es requerido.");

            if (dto.PersonId <= 0)
                throw new BusinessException("PersonId es requerido y debe ser válido.");

            if (string.IsNullOrWhiteSpace(dto.Password))
                throw new BusinessException("La contraseña es requerida para crear el usuario.");

            // Unicidad
            if (await _userRepository.ExistsByEmailAsync(dto.Email.Trim()))
                throw new BusinessException("El correo ya está registrado.");

            // Validar existencia de la persona
            if (await _personRepository.GetByIdAsync(dto.PersonId) is null)
                throw new BusinessException("No existe la persona asociada al PersonId proporcionado.");

            // Mapear y hashear
            var user = _mapper.Map<User>(dto);
            user.Email = dto.Email.Trim();
            user.Password = _passwordHasher.HashPassword(user, dto.Password!);

            // Persistir
            var created = await _userRepository.AddAsync(user);

            // Retornar DTO del registro creado
            return _mapper.Map<UserSelectDto>(created);
        }


        // ======================================================
        // ACTUALIZAR (NO toca password ni datos de Person)
        // ======================================================

        public override async Task<UserSelectDto> UpdateAsync(UserUpdateDto dto)
        {
            if (dto is null) throw new BusinessException("Datos requeridos.");

            if (string.IsNullOrWhiteSpace(dto.Email))
                throw new BusinessException("El correo es requerido.");

            var user = await _userRepository.GetByIdAsync(dto.Id)
                       ?? throw new BusinessException("Usuario no encontrado.");

            if (await _userRepository.ExistsByEmailAsync(dto.Email.Trim(), excludeId: dto.Id))
                throw new BusinessException("El correo ya está registrado por otro usuario.");

            // Mapear cambios
            _mapper.Map(dto, user);
            user.Email = dto.Email.Trim();

            // Validar cambio de PersonId si aplica
            if (dto.PersonId > 0 && dto.PersonId != user.PersonId)
            {
                if (await _personRepository.GetByIdAsync(dto.PersonId) is null)
                    throw new BusinessException("El nuevo PersonId no existe.");
                user.PersonId = dto.PersonId;
            }

            // Persistir cambios
            var updated = await _userRepository.UpdateAsync(user);

            // Retornar DTO actualizado
            return _mapper.Map<UserSelectDto>(updated);
        }


        // ======================================================
        // Cambiar contraseña explícitamente
        // ======================================================

        public async Task SetPasswordAsync(int userId, string newPassword)
        {
            if (userId <= 0) throw new BusinessException("Id inválido.");
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new BusinessException("La nueva contraseña es requerida.");

            var user = await _userRepository.GetByIdAsync(userId)
                       ?? throw new BusinessException("Usuario no encontrado.");

            user.Password = _passwordHasher.HashPassword(user, newPassword);
            await _userRepository.UpdateAsync(user);
        }

        // ======================================================
        // Buscar usuario por el ID de la Persona
        // ======================================================

        public async Task<UserSelectDto?> GetByPersonIdAsync(int personId, CancellationToken ct = default)
        {
            var entity = await _userRepository.GetByPersonIdAsync(personId, ct);
            return _mapper.Map<UserSelectDto>(entity);
        }

        // ======================================================
        // Búsqueda / Ordenación (si tu BusinessGeneric lo requiere)
        // ======================================================

        protected override Expression<Func<User, string?>>[] SearchableFields() =>
        [
            x => x.Email,
            x => x.Person.FirstName,
            x => x.Person.LastName,
            x => x.Person.Document,
            x => x.Person.Phone,
            x => x.Person.Address
        ];

        protected override string[] SortableFields() => new[]
        {
            nameof(User.Email),
            "Person.Document",
            "Person.Phone",
            "Person.Address",
            nameof(User.Active),
            nameof(User.CreatedAt),
            nameof(User.Id)
        };
    }
}
