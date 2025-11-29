using Data.Services.Utilities;
using Entity.Domain.Models.Implements.Utilities;
using Entity.Enum;
using Entity.Infrastructure.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Test.Modulo.Data;

public class ImagesRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task GetAllReturnsOnlyActiveNotDeletedOrdered()
    {
        await using var ctx = CreateContext();
        var repo = new ImagesRepository(ctx);

        ctx.Images.AddRange(
            new Images
            {
                FileName = "a.jpg",
                FilePath = "/a",
                PublicId = "p1",
                EntityType = EntityType.Establishment,
                EntityId = 1,
                Active = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Images
            {
                FileName = "b.jpg",
                FilePath = "/b",
                PublicId = "p2",
                EntityType = EntityType.Establishment,
                EntityId = 1,
                Active = false,
                IsDeleted = false
            },
            new Images
            {
                FileName = "c.jpg",
                FilePath = "/c",
                PublicId = "p3",
                EntityType = EntityType.Establishment,
                EntityId = 1,
                Active = true,
                IsDeleted = true
            },
            new Images
            {
                FileName = "d.jpg",
                FilePath = "/d",
                PublicId = "p4",
                EntityType = EntityType.Establishment,
                EntityId = 1,
                Active = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            }
        );

        await ctx.SaveChangesAsync();

        var all = (await repo.GetAllAsync()).ToList();
        all.Should().HaveCount(2);
        all.First().PublicId.Should().Be("p4");
        all.Last().PublicId.Should().Be("p1");
    }

    [Fact]
    public async Task GetByEstablishmentFiltersAndOrders()
    {
        await using var ctx = CreateContext();
        var repo = new ImagesRepository(ctx);

        ctx.Images.AddRange(
            new Images
            {
                FileName = "a.jpg",
                FilePath = "/a",
                PublicId = "p1",
                EntityType = EntityType.Establishment,
                EntityId = 9,
                Active = true
            },
            new Images
            {
                FileName = "b.jpg",
                FilePath = "/b",
                PublicId = "p2",
                EntityType = EntityType.Establishment,
                EntityId = 9,
                Active = true
            },
            new Images
            {
                FileName = "c.jpg",
                FilePath = "/c",
                PublicId = "p3",
                EntityType = EntityType.Establishment,
                EntityId = 8,
                Active = true
            }
        );

        await ctx.SaveChangesAsync();

        var list = await repo.GetByAsync("Establishment", 9);

        list.Should().HaveCount(2);
        list.First().PublicId.Should().Be("p2"); // Id desc
    }

    [Fact]
    public async Task AddRangeAndDeleteByPublicIdWorks()
    {
        await using var ctx = CreateContext();
        var repo = new ImagesRepository(ctx);

        await repo.AddRangeAsync(new[]
        {
            new Images
            {
                FileName = "a.jpg",
                FilePath = "/a",
                PublicId = "pa",
                EntityType = EntityType.Establishment,
                EntityId = 1
            },
            new Images
            {
                FileName = "b.jpg",
                FilePath = "/b",
                PublicId = "pb",
                EntityType = EntityType.Establishment,
                EntityId = 1
            }
        });

        (await repo.DeleteByPublicIdAsync("pa")).Should().BeTrue();
        (await repo.DeleteByPublicIdAsync("nope")).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteLogicalByPublicIdWorks()
    {
        await using var ctx = CreateContext();
        var repo = new ImagesRepository(ctx);

        await repo.AddRangeAsync(new[]
        {
            new Images
            {
                FileName = "a.jpg",
                FilePath = "/a",
                PublicId = "pa",
                EntityType = EntityType.Establishment,
                EntityId = 1
            }
        });

        (await repo.DeleteLogicalByPublicIdAsync("pa")).Should().BeTrue();
        (await repo.DeleteLogicalByPublicIdAsync("")).Should().BeFalse();
    }
}

