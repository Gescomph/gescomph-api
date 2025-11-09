using Data.Interfaz.IDataImplement.SecurityAuthentication;
using Data.Repository;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.SecurityAuthentication
{
    public class UserRepository : DataGeneric<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context) { }

        // =========================
        // Base Query
        // =========================
        private IQueryable<User> BaseQuery(bool track = false)
        {
            var query = track ? _dbSet : _dbSet.AsNoTracking();
            return query.Where(u => !u.IsDeleted);
        }

        // =========================
        // Listados
        // =========================
        public override IQueryable<User> GetAllQueryable()
        {
            return BaseQuery()
                .Include(u => u.Person).ThenInclude(p => p.City)
                .Include(u => u.RolUsers).ThenInclude(ru => ru.Rol)
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id);
        }

        public override async Task<IEnumerable<User>> GetAllAsync()
        {
            return await GetAllQueryable().ToListAsync();
        }

        // =========================
        // POR ID
        // =========================
        public override async Task<User?> GetByIdAsync(int id)
        {
            return await BaseQuery(track: true)
                .Include(u => u.Person).ThenInclude(p => p.City)
                .Include(u => u.RolUsers).ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // =========================
        // POR PERSONA
        // =========================
        public async Task<User?> GetByPersonIdAsync(int personId)
        {
            return await BaseQuery()
                .Include(u => u.Person).ThenInclude(p => p.City)
                .Include(u => u.RolUsers).ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => u.PersonId == personId);
        }

        // =========================
        // POR EMAIL (email normalizado en capa Business)
        // =========================
        public async Task<bool> ExistsByEmailAsync(string normalizedEmail, int? excludeId = null)
        {
            var query = BaseQuery().Where(u => u.Email == normalizedEmail);
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<int?> GetIdByEmailAsync(string normalizedEmail)
        {
            return await BaseQuery()
                .Where(u => u.Email == normalizedEmail)
                .Select(u => (int?)u.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string normalizedEmail)
        {
            return await BaseQuery()
                .Include(u => u.Person)
                .Include(u => u.RolUsers).ThenInclude(ru => ru.Rol)
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        }

        // Solo datos mínimos para autenticación
        public async Task<User?> GetAuthUserByEmailAsync(string normalizedEmail)
        {
            return await BaseQuery()
                .Select(u => new User
                {
                    Id = u.Id,
                    Email = u.Email,
                    Password = u.Password, // hash
                    PersonId = u.PersonId,
                    TwoFactorEnabled = u.TwoFactorEnabled
                })
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        }
    }
}
