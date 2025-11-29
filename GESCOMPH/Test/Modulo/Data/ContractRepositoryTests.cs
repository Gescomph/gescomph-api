using Data.Services.Business;
using Entity.Domain.Models.Implements.Business;
using Entity.Domain.Models.Implements.Persons;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class ContractRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static Person CreatePerson(int id)
        => new Person
        {
            Id = id,
            FirstName = $"P{id}",
            LastName = "L",
            CityId = 1
        };

    [Fact]
    public async Task GetByPersonAsyncFiltersByPersonAndLoadsRelations()
    {
        await using var ctx = CreateContext();
        var repo = new ContractRepository(ctx);

        var p1 = CreatePerson(1);
        var p2 = CreatePerson(2);

        var u1 = new User { Id = 1, Email = "p1@mail", Password = "x", Person = p1 };
        var u2 = new User { Id = 2, Email = "p2@mail", Password = "x", Person = p2 };

        p1.User = u1;
        p2.User = u2;

        ctx.Persons.AddRange(p1, p2);
        ctx.Users.AddRange(u1, u2);

        var now = DateTime.UtcNow;

        ctx.Contracts.AddRange(
            new Contract
            {
                Id = 1,
                PersonId = 1,
                Person = p1,
                StartDate = now.AddMonths(-2),
                EndDate = now.AddMonths(1),
                Active = true,
                IsDeleted = false,
                CreatedAt = now.AddDays(-1)
            },
            new Contract
            {
                Id = 2,
                PersonId = 2,
                Person = p2,
                StartDate = now.AddMonths(-1),
                EndDate = now.AddMonths(2),
                Active = true,
                IsDeleted = false,
                CreatedAt = now
            }
        );

        await ctx.SaveChangesAsync();

        var result = (await repo.GetByPersonAsync(1)).ToList();

        result.Should().ContainSingle();
        result.Single().Person!.User!.Email.Should().Be("p1@mail");
    }
}
