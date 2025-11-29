using Business.Interfaces.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.User;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebGESCOMPH.Controllers.Module.SecurityAuthentication;

namespace Test.Modulo.Web;

public class UserControllerTests
{
    private readonly Mock<IUserService> _svc = new();
    private readonly Mock<ILogger<UserController>> _logger = new();

    private UserController Create() => new(_svc.Object, _logger.Object);

    // =============================================================
    // GET BY ID
    // =============================================================
    [Fact]
    public async Task GetByIdNotFound()
    {
        _svc.Setup(s => s.GetByIdAsync(7))
            .ReturnsAsync((UserSelectDto?)null);

        var res = await Create().GetById(7);

        Assert.IsType<NotFoundResult>(res.Result);
    }

    // =============================================================
    // POST
    // =============================================================
    [Fact]
    public async Task PostOk()
    {
        var created = new UserSelectDto { Id = 5, Email = "e@mail" };

        _svc.Setup(s => s.CreateAsync(It.IsAny<UserCreateDto>()))
            .ReturnsAsync(created);

        var res = await Create().Post(new UserCreateDto
        {
            Email = "e@mail",
            Password = "A",
            PersonId = 10,
            RoleIds = new List<int> { 1, 2 }
        });

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var dto = Assert.IsType<UserSelectDto>(ok.Value);

        Assert.Equal(5, dto.Id);
    }

    // =============================================================
    // PUT
    // =============================================================
    [Fact]
    public async Task PutOk()
    {
        var updated = new UserSelectDto { Id = 6, Email = "e@mail" };

        _svc.Setup(s => s.UpdateAsync(It.IsAny<UserUpdateDto>()))
            .ReturnsAsync(updated);

        var res = await Create().Put(6, new UserUpdateDto
        {
            Email = "e@mail",
            PersonId = 99
        });

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var dto = Assert.IsType<UserSelectDto>(ok.Value);

        Assert.Equal(6, dto.Id);
    }

    [Fact]
    public async Task PutNotFound()
    {
        _svc.Setup(s => s.UpdateAsync(It.IsAny<UserUpdateDto>()))
            .ReturnsAsync((UserSelectDto?)null);

        var res = await Create().Put(10, new UserUpdateDto
        {
            Email = "no@mail",
            PersonId = 99
        });

        Assert.IsType<NotFoundResult>(res.Result);
    }

    // =============================================================
    // GET ALL
    // =============================================================
    [Fact]
    public async Task GetReturnsOk()
    {
        _svc.Setup(s => s.GetAllAsync())
            .ReturnsAsync(new List<UserSelectDto> { new() { Id = 1, Email = "a@mail" } });

        var res = await Create().Get();

        var ok = Assert.IsType<OkObjectResult>(res.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<UserSelectDto>>(ok.Value);

        Assert.Single(list);
    }

    // =============================================================
    // DELETE
    // =============================================================
    [Fact]
    public async Task DeleteNoContentWhenDeleted()
    {
        _svc.Setup(s => s.DeleteAsync(3))
            .ReturnsAsync(true);

        var res = await Create().Delete(3);

        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task DeleteNotFound()
    {
        _svc.Setup(s => s.DeleteAsync(5))
            .ReturnsAsync(false);

        var res = await Create().Delete(5);

        Assert.IsType<NotFoundResult>(res);
    }

    // =============================================================
    // DELETE LOGIC
    // =============================================================
    [Fact]
    public async Task DeleteLogicNoContentWhenDeleted()
    {
        _svc.Setup(s => s.DeleteLogicAsync(4))
            .ReturnsAsync(true);

        var res = await Create().DeleteLogic(4);

        Assert.IsType<NoContentResult>(res);
    }

    [Fact]
    public async Task DeleteLogicNotFound()
    {
        _svc.Setup(s => s.DeleteLogicAsync(9))
            .ReturnsAsync(false);

        var res = await Create().DeleteLogic(9);

        Assert.IsType<NotFoundResult>(res);
    }

    // =============================================================
    // CHANGE ACTIVE STATUS
    // =============================================================
    [Fact]
    public async Task ChangeActiveStatusNoContent()
    {
        var res = await Create().ChangeActiveStatus(
            5,
            new WebGESCOMPH.Contracts.Requests.ChangeActiveStatusRequest
            {
                Active = true
            }
        );

        Assert.IsType<NoContentResult>(res);
    }

    // =============================================================
    // SOFT DELETE
    // =============================================================
    [Fact]
    public async Task SoftDeleteOk()
    {
        var res = await Create().SoftDelete(5);

        var ok = Assert.IsType<OkObjectResult>(res);
        Assert.Contains("eliminados lógicamente", ok.Value!.ToString());
    }
}
