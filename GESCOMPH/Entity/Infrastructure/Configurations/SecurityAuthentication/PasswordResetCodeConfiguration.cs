using Entity.DTOs.Implements.SecurityAuthentication.Auth.RestPasword;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.SecurityAuthentication
{
    public class PasswordResetCodeConfiguration : BaseModelConfiguration<PasswordResetCode>
    {
        public override void Configure(EntityTypeBuilder<PasswordResetCode> builder)
        {
            base.Configure(builder);

            builder.ToTable("PasswordResetCodes", DatabaseSchemas.SecurityAuthentication);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Expiration)
                   .IsRequired();

            builder.Property(x => x.IsUsed)
                   .IsRequired();

            builder.HasIndex(x => x.Email);
            builder.HasIndex(x => x.Code);
            builder.HasIndex(x => new { x.Email, x.IsUsed });
        }
    }
}
