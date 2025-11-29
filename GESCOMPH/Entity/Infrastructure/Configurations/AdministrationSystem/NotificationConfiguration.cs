using Entity.Domain.Models.Implements.AdministrationSystem;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.AdministrationSystem
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Esquema + Tabla
            builder.ToTable("Notifications", "AdministrationSystem");

            // Clave primaria
            builder.HasKey(n => n.Id);

            // Campos obligatorios
            builder.Property(n => n.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(n => n.Message)
                .IsRequired();

            builder.Property(n => n.Type)
                .IsRequired();

            builder.Property(n => n.Priority)
                .IsRequired();

            builder.Property(n => n.Status)
                .IsRequired();

            // FK hacia Users
            builder.HasOne(n => n.RecipientUser)
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Índice compuesto CreatedAt + Id (optimiza feed)
            builder.HasIndex(n => new { n.CreatedAt, n.Id })
                   .HasDatabaseName("IX_Notifications_CreatedAt_Id");
        }
    }
}
