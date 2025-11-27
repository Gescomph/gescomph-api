using Data.Services.SecurityAuthentication;
using Entity.Domain.Models.Implements.Location;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class UserRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static User NewUser(int id, string email)
        => new()
        {
            Id = id,
            Email = email,
            Password = "hash",
            PersonId = id
        };

    // ======================================================
    //  EXISTS / GET ID
    // ======================================================
    [Fact]
    public async Task ExistsByEmailVariants()
    {
        await using var ctx = CreateContext();
        var repo = new UserRepository(ctx);

        ctx.Set<Department>().Add(new Department { Id = 1, Name = "Dept" });
        ctx.Set<City>().Add(new City { Id = 1, Name = "Neiva", DepartmentId = 1 });

        ctx.Persons.Add(new Entity.Domain.Models.Implements.Persons.Person
        {
            Id = 1,
            FirstName = "A",
            LastName = "B",
            CityId = 1
        });

        ctx.Persons.Add(new Entity.Domain.Models.Implements.Persons.Person
        {
            Id = 2,
            FirstName = "C",
            LastName = "D",
            CityId = 1
        });

        ctx.Users.AddRange(NewUser(1, "a@mail"), NewUser(2, "b@mail"));
        await ctx.SaveChangesAsync();

        (await repo.ExistsByEmailAsync("a@mail")).Should().BeTrue();

        (await repo.ExistsByEmailAsync("a@mail", excludeId: 1)).Should().BeFalse();
        (await repo.ExistsByEmailAsync("a@mail", excludeId: 2)).Should().BeTrue();

        (await repo.GetIdByEmailAsync("b@mail")).Should().Be(2);
    }

    // ======================================================
    //  GET BY EMAIL
    // ======================================================
    [Fact]
    public async Task GetByEmailVariants()
    {
        await using var ctx = CreateContext();
        var repo = new UserRepository(ctx);

        ctx.Set<Department>().Add(new Department { Id = 1, Name = "Dept" });
        ctx.Set<City>().Add(new City { Id = 1, Name = "Neiva", DepartmentId = 1 });

        ctx.Persons.Add(new Entity.Domain.Models.Implements.Persons.Person
        {
            Id = 1,
            FirstName = "A",
            LastName = "B",
            CityId = 1
        });

        ctx.Users.Add(NewUser(1, "a@mail"));
        await ctx.SaveChangesAsync();

        var full = await repo.GetByEmailAsync("a@mail");
        full.Should().NotBeNull();
        full!.PersonId.Should().Be(1);

        var auth = await repo.GetAuthUserByEmailAsync("a@mail");
        auth.Should().NotBeNull();
        auth!.Password.Should().Be("hash");
    }

    // ======================================================
    //  GET BY PERSON ID
    // ======================================================
    [Fact]
    public async Task GetByPersonIdReturnsUser()
    {
        await using var ctx = CreateContext();
        var repo = new UserRepository(ctx);

        ctx.Set<Department>().Add(new Department { Id = 1, Name = "Dept" });
        ctx.Set<City>().Add(new City { Id = 1, Name = "Neiva", DepartmentId = 1 });

        ctx.Persons.Add(new Entity.Domain.Models.Implements.Persons.Person
        {
            Id = 1,
            FirstName = "A",
            LastName = "B",
            CityId = 1
        });

        ctx.Users.Add(NewUser(1, "a@mail"));
        await ctx.SaveChangesAsync();

        var user = await repo.GetByPersonIdAsync(1);

        user.Should().NotBeNull();
        user!.Id.Should().Be(1);
    }

    // ======================================================
    //  GET BY ID
    // ======================================================
    [Fact]
    public async Task GetByIdReturnsUserWithIncludes()
    {
        await using var ctx = CreateContext();
        var repo = new UserRepository(ctx);

        ctx.Set<Department>().Add(new Department { Id = 1, Name = "Dept" });
        ctx.Set<City>().Add(new City { Id = 1, Name = "Neiva", DepartmentId = 1 });

        ctx.Persons.Add(new Entity.Domain.Models.Implements.Persons.Person
        {
            Id = 1,
            FirstName = "A",
            LastName = "B",
            CityId = 1
        });

        ctx.Users.Add(NewUser(1, "a@mail"));
        await ctx.SaveChangesAsync();

        var user = await repo.GetByIdAsync(1);

        user.Should().NotBeNull();
        user!.Person.Should().NotBeNull();
        user.Person.City.Should().NotBeNull();
    }
}
