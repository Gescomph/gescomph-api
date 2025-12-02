using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.Business
{
    public class AppointmentConfiguration : BaseModelConfiguration<Appointment>
    {
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            base.Configure(builder);

            builder.ToTable("Appointments", DatabaseSchemas.Business);

            builder.Property(a => a.Description)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(a => a.RequestDate)
                .IsRequired();

            builder.Property(a => a.DateTimeAssigned);

            builder.Property(a => a.Observation)
                .HasMaxLength(500);

            builder.Property(a => a.Status)
                .IsRequired()
                .HasDefaultValue(Entity.Enum.Status.Pendiente);

            builder.HasOne(a => a.Person)
                .WithMany(p => p.Appointments)
                .HasForeignKey(a => a.PersonId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(a => a.Establishment)
                .WithMany(e => e.Appointments)
                .HasForeignKey(a => a.EstablishmentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(a => a.PersonId);
            builder.HasIndex(a => a.EstablishmentId);
            builder.HasIndex(a => new { a.PersonId, a.EstablishmentId, a.RequestDate });
        }
    }
}
