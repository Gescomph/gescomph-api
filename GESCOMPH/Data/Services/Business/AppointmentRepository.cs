using Data.Interfaz.IDataImplement.Business;
using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Data.Services.Business
{
    public class AppointmentRepository(ApplicationDbContext context) : DataGeneric<Appointment>(context), IAppointmentRepository
    {

        public override async Task<IEnumerable<Appointment>> GetAllAsync()
        {
            return await _dbSet
                .Where(e => !e.IsDeleted)
                .OrderByDescending(e => e.CreatedAt)
                .ThenByDescending(e => e.Id)
                .Include(e => e.Establishment)
                .Include(e => e.Person)
                .ToListAsync();
        }

        public override async Task<Appointment?> GetByIdAsync(int id)
        {
            return await _dbSet
                .Where(e => e.Id == id && !e.IsDeleted)
                .Include (e => e.Establishment)
                .Include (e => e.Person)
                    .ThenInclude(p => p.User)
                .FirstOrDefaultAsync();

        }

        public async Task<IEnumerable<Appointment>> GetAppointmentByDate(int year, int month, int day)
        {
            return await _dbSet
                .Include(e => e.Establishment)
                .Include(e => e.Person)
                .Where(e => e.DateTimeAssigned != null
                     && e.DateTimeAssigned.Value.Year == year
                     && e.DateTimeAssigned.Value.Month == month
                     && e.DateTimeAssigned.Value.Day == day)
                .ToListAsync();
        }

        public async Task<IEnumerable<Appointment>> GetAllByPersonId(int personId)
        {
            return await _dbSet
                .Include(e => e.Establishment)
                .Include(e => e.Person)
                .Where(e => e.PersonId == personId)
                .ToListAsync();
        }

    }
}
