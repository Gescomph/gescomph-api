using Data.Repository;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class SystemParameterRepositoryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddGetAllUpdateDeleteLogicWorks()
    {
        var db = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(db);
        var repo = new DataGeneric<SystemParameter>(ctx);

        var p = await repo.AddAsync(new SystemParameter
        {
            Key = "UVT",
            Value = "49798.75",
            EffectiveFrom = DateTime.UtcNow
        });

        (await repo.GetAllAsync()).Should().ContainSingle(x => x.Id == p.Id);

        p.Value = "50000";
        await repo.UpdateAsync(p);

        var again = await repo.GetByIdAsync(p.Id);
        again!.Value.Should().Be("50000");

        (await repo.DeleteLogicAsync(p.Id)).Should().BeTrue();

        (await repo.GetByIdAsync(p.Id)).Should().BeNull();
    }
}
