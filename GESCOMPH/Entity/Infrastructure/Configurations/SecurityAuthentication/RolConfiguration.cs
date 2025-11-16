using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Configurations.Base;
using Entity.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.SecurityAuthentication
{
    public class RolConfiguration : BaseModelGenericConfiguration<Rol>
    {
        public override void Configure(EntityTypeBuilder<Rol> builder)
        {
            base.Configure(builder); // Aplica: Id, Active, IsDeleted, CreatedAt, Name, Description

            builder.ToTable("Roles", DatabaseSchemas.SecurityAuthentication);

            // Relación con RolUser (1:n)
            builder.HasMany(r => r.RolUsers)
                .WithOne(ru => ru.Rol)
                .HasForeignKey(ru => ru.RolId)
                .OnDelete(DeleteBehavior.Cascade); // Puedes ajustar a Restrict si no quieres eliminar en cascada

            // Relación con RolFormPermission (1:n)
            builder.HasMany(r => r.RolFormPermissions)
                .WithOne(rfp => rfp.Rol)
                .HasForeignKey(rfp => rfp.RolId)
                .OnDelete(DeleteBehavior.Cascade); // Idem arriba
        }
    }
}
