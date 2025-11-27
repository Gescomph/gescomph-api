using Data.Repository;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class ModuleRepositoryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddGetAllDeleteWorks()
    {
        var db = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(db);
        var repo = new DataGeneric<Module>(ctx);

        var m = await repo.AddAsync(new Module { Name = "M", Description = "D", Icon = "mdi-home" });

        (await repo.GetAllAsync()).Should().ContainSingle(x => x.Id == m.Id);

        (await repo.DeleteAsync(m.Id)).Should().BeTrue();

        (await repo.GetByIdAsync(m.Id)).Should().BeNull();
    }
}
