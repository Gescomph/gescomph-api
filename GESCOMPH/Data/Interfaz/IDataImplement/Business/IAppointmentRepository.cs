using Data.Interfaz.DataBasic;
using Entity.Domain.Models.Implements.Business;
using Entity.DTOs.Implements.Business.Appointment;

namespace Data.Interfaz.IDataImplement.Business
{
    public interface IAppointmentRepository : IDataGeneric<Appointment>
    {
        Task<IEnumerable<Appointment>> GetAppointmentByDate(int year, int month, int day);
        Task<IEnumerable<Appointment>> GetAllByPersonId(int personId);
    }
}
