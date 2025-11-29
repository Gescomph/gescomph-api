using Data.Repository;
using Entity.Domain.Models.Implements.Persons;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class PersonRepositoryTests
{
    private static ApplicationDbContext CreateContext(string name)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(name)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddGetDeleteWorks()
    {
        var db = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(db);
        var repo = new DataGeneric<Person>(ctx);

        var p = await repo.AddAsync(new Person { FirstName = "A", LastName = "B" });

        (await repo.GetByIdAsync(p.Id)).Should().NotBeNull();

        (await repo.DeleteAsync(p.Id)).Should().BeTrue();

        (await repo.GetByIdAsync(p.Id)).Should().BeNull();
    }
}

