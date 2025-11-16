using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Configurations;
using Entity.Infrastructure.Configurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Entity.Infrastructure.Configurations.SecurityAuthentication
{
    public class TwoFactorCodeConfiguration : BaseModelConfiguration<TwoFactorCode>
    {
        public override void Configure(EntityTypeBuilder<TwoFactorCode> builder)
        {
            base.Configure(builder);

            builder.ToTable("TwoFactorCodes", DatabaseSchemas.SecurityAuthentication);

            builder.Property(x => x.Code)
                .IsRequired()
                .HasMaxLength(16);

            builder.Property(x => x.ExpiresAt)
                .IsRequired();

            builder.Property(x => x.IsUsed)
                .IsRequired();

            builder.Property(x => x.UsedAt);

            builder.HasOne(x => x.User)
                .WithMany(u => u.TwoFactorCodes)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);
            builder.HasIndex(x => x.Code);
            builder.HasIndex(x => new { x.UserId, x.Code });
        }
    }
}
