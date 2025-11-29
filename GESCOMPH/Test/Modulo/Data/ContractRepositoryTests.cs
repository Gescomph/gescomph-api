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

    [Fact(Skip="EFCore.InMemory no soporta ExecuteUpdate/ExecuteUpdateAsync")]
    public async Task DeactivateExpiredAsync_DisablesActiveEndedContracts()
    {
        await using var ctx = Ctx();
        var repo = new ContractRepository(ctx);
        var now = DateTime.UtcNow;

        ctx.Persons.Add(NewPerson(1));
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

        // Contract 1 expired and inactive on est 10
        ctx.Contracts.Add(new Contract { Id = 1, PersonId = 1, StartDate = now.AddMonths(-3), EndDate = now.AddDays(-1), Active = false });
        ctx.PremisesLeaseds.Add(new PremisesLeased { Id = 1, ContractId = 1, EstablishmentId = 10 });

        // Contract 2 active (should keep est 11 occupied)
        ctx.Contracts.Add(new Contract { Id = 2, PersonId = 1, StartDate = now.AddMonths(-1), EndDate = now.AddMonths(1), Active = true });
        ctx.PremisesLeaseds.Add(new PremisesLeased { Id = 2, ContractId = 2, EstablishmentId = 11 });

        await ctx.SaveChangesAsync();

        var result = (await repo.GetByPersonAsync(1)).ToList();

        result.Should().ContainSingle();
        result.Single().Person!.User!.Email.Should().Be("p1@mail");
    }
}
