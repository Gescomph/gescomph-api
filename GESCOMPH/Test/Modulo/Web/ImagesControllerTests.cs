using Business.Interfaces.Implements.Utilities;
using Entity.DTOs.Implements.Utilities.Images;
using Entity.Enum;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebGESCOMPH.Controllers.Module.Utilities;

namespace Test.Modulo.Web;

public class ImagesControllerTests
{
    private readonly Mock<IImagesService> _service = new();

    private ImagesController Create() => new(_service.Object);

    // -----------------------------------------------------------
    // Upload: BadRequest when no files
    // -----------------------------------------------------------
    [Fact]
    public async Task UploadReturnsBadRequestWhenNoFiles()
    {
        var controller = Create();
        var files = new FormFileCollection();

        var result = await controller.Upload("Establishment", 1, files);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // -----------------------------------------------------------
    // Upload: OK
    // -----------------------------------------------------------
    [Fact]
    public async Task UploadReturnsOkWithResult()
    {
        _service.Setup(s => s.AddImagesAsync("Establishment", 1, It.IsAny<IFormFileCollection>()))
            .ReturnsAsync(new List<ImageSelectDto>
            {
                new ImageSelectDto(
                    1,
                    "a.jpg",
                    "/a",
                    "pid",
                    EntityType.Establishment,
                    1
                )
            });

        var files = new FormFileCollection
        {
            new FormFile(Stream.Null, 0, 0, "f", "a.jpg")
        };

        var result = await Create().Upload("Establishment", 1, files);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // -----------------------------------------------------------
    // Delete by publicId
    // -----------------------------------------------------------
    [Fact]
    public async Task DeleteByPublicIdReturnsNoContent()
    {
        _service.Setup(s => s.DeleteByPublicIdAsync("pid"))
                .Returns(Task.CompletedTask);

        var result = await Create().Delete("pid");

        Assert.IsType<NoContentResult>(result);
    }

    // -----------------------------------------------------------
    // GetImages OK
    // -----------------------------------------------------------
    [Fact]
    public async Task GetImagesReturnsOk()
    {
        _service.Setup(s => s.GetImagesAsync("Establishment", 2))
            .ReturnsAsync(new List<ImageSelectDto>
            {
                new ImageSelectDto(
                    3,
                    "f.jpg",
                    "/f",
                    "p3",
                    EntityType.Establishment,
                    2
                )
            });

        var result = await Create().GetImages("Establishment", 2);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    // -----------------------------------------------------------
    // Delete by Id
    // -----------------------------------------------------------
    [Fact]
    public async Task DeleteByIdReturnsNoContent()
    {
        _service.Setup(s => s.DeleteByIdAsync(10))
                .Returns(Task.CompletedTask);

        var result = await Create().DeleteById(10);

        Assert.IsType<NoContentResult>(result);
    }
}
