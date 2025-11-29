using Data.Repository;
using Entity.Domain.Models.Implements.AdministrationSystem;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class FormRepositoryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddUpdateDeleteLogicWorks()
    {
        var db = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(db);
        var repo = new DataGeneric<Form>(ctx);

        var f = await repo.AddAsync(new Form { Name = "Menu", Description = "D", Route = "/home" });
        (await repo.GetByIdAsync(f.Id)).Should().NotBeNull();

        f.Description = "D2";
        var updated = await repo.UpdateAsync(f);
        updated.Description.Should().Be("D2");

        (await repo.DeleteLogicAsync(f.Id)).Should().BeTrue();
        (await repo.GetByIdAsync(f.Id)).Should().BeNull();
    }
}

