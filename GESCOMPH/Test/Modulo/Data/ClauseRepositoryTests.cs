using Data.Repository;
using Entity.Domain.Models.Implements.Business;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class ClauseRepositoryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddUpdateSoftDeleteWorks()
    {
        var db = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(db);
        var repo = new DataGeneric<Clause>(ctx);

        var created = await repo.AddAsync(new Clause { Name = "N", Description = "D" });
        created.Id.Should().BeGreaterThan(0);

        created.Description = "D2";
        var updated = await repo.UpdateAsync(created);
        updated.Description.Should().Be("D2");

        (await repo.DeleteLogicAsync(updated.Id)).Should().BeTrue();
        (await repo.GetByIdAsync(updated.Id)).Should().BeNull();
    }
}

