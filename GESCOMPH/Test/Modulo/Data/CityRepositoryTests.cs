using Data.Services.Location;
using Entity.Domain.Models.Implements.Location;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class CityRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetCityByDepartmentAsyncFiltersByDepartment()
    {
        await using var ctx = CreateContext();
        var repo = new CityRepository(ctx);

        ctx.Set<Department>().Add(new Department { Id = 1, Name = "Huila" });
        ctx.Set<Department>().Add(new Department { Id = 2, Name = "Antioquia" });

        ctx.Set<City>().AddRange(
            new City { Id = 1, Name = "Neiva", DepartmentId = 1 },
            new City { Id = 2, Name = "Medellin", DepartmentId = 2 }
        );

        await ctx.SaveChangesAsync();

        var list = await repo.GetCityByDepartmentAsync(1);
        list.Should().ContainSingle(c => c.Name == "Neiva");
    }
}
