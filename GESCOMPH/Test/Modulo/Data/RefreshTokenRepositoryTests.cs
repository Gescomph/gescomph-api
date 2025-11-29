using Data.Services.SecurityAuthentication;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class RefreshTokenRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddGetByHashRevokeAndGetValidByUser()
    {
        await using var ctx = CreateContext();
        var repo = new RefreshTokenRepository(ctx);

        var t1 = new RefreshToken
        {
            UserId = 1,
            TokenHash = "h1",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var t2 = new RefreshToken
        {
            UserId = 1,
            TokenHash = "h2",
            ExpiresAt = DateTime.UtcNow.AddHours(-1)
        };

        await repo.AddAsync(t1);
        await repo.AddAsync(t2);

        // *** NO USAMOS EL INCLUDE EN EL TEST ***
        var fetched = await ctx.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == "h1");
        fetched.Should().NotBeNull();

        // v�lidos: solo h1 (no revocado y no expirado)
        var valid = await repo.GetValidTokensByUserAsync(1);
        valid.Should().ContainSingle(v => v.TokenHash == "h1");

        await repo.RevokeAsync(fetched!, "h3");

        var validAfter = await repo.GetValidTokensByUserAsync(1);
        validAfter.Should().BeEmpty();
    }
}

