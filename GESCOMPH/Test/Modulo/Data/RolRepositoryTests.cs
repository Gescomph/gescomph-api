using Data.Repository;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.Domain.Models.ModelBase;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class RolRepositoryTests
{
    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task AddAndGetByIdWorksAndRespectsSoftDelete()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);
        var repo = new DataGeneric<Rol>(ctx);

        var created = await repo.AddAsync(new Rol { Name = "Admin", Description = "desc" });
        created.Id.Should().BeGreaterThan(0);

        var fetched = await repo.GetByIdAsync(created.Id);
        fetched.Should().NotBeNull();

        // Soft delete oculta en GetByIdAsync
        await repo.DeleteLogicAsync(created.Id);
        var afterSoftDelete = await repo.GetByIdAsync(created.Id);
        afterSoftDelete.Should().BeNull();
    }

    [Fact]
    public async Task GetAllExcludesDeletedAndOrdersByCreatedAtDescThenIdDesc()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);
        var repo = new DataGeneric<Rol>(ctx);

        var older = await repo.AddAsync(new Rol
        {
            Name = "A",
            Description = "",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        });

        var newer1 = await repo.AddAsync(new Rol
        {
            Name = "B",
            Description = "",
            CreatedAt = DateTime.UtcNow
        });

        var newer2 = await repo.AddAsync(new Rol
        {
            Name = "C",
            Description = "",
            CreatedAt = DateTime.UtcNow
        });

        await repo.DeleteLogicAsync(older.Id);

        var all = (await repo.GetAllAsync()).ToList();
        all.Should().HaveCount(2);

        all.Select(x => x.Name).Should().Equal(new[]
        {
            newer2.Name,
            newer1.Name
        });
    }

    [Fact]
    public async Task UpdatePreservesIdAndCreatedAt()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);
        var repo = new DataGeneric<Rol>(ctx);

        var created = await repo.AddAsync(new Rol { Name = "Old", Description = "D" });
        var originalId = created.Id;
        var originalCreatedAt = created.CreatedAt;

        var updated = await repo.UpdateAsync(new Rol
        {
            Id = originalId,
            Name = "New",
            Description = "ND",
            CreatedAt = DateTime.UtcNow.AddYears(-10)
        });

        updated.Id.Should().Be(originalId);
        updated.CreatedAt.Should().Be(originalCreatedAt);
        updated.Name.Should().Be("New");
        updated.Description.Should().Be("ND");
    }

    [Fact]
    public async Task DeleteRemovesEntity()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var ctx = CreateContext(dbName);
        var repo = new DataGeneric<Rol>(ctx);

        var created = await repo.AddAsync(new Rol { Name = "X", Description = "Y" });

        (await repo.DeleteAsync(created.Id)).Should().BeTrue();
        (await repo.DeleteAsync(created.Id)).Should().BeFalse();
    }
}

