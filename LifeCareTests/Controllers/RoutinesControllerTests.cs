using System.Security.Claims;
using FluentAssertions;
using LifeCare.Controllers;
using LifeCare.Models;
using LifeCare.Services.Interfaces;
using LifeCare.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LifeCareTests.Controllers;

public class RoutinesControllerTests
{
    private static UserManager<User> MockUserManager()
    {
        var store = new Mock<IUserStore<User>>();
        return new UserManager<User>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static ClaimsPrincipal BuildPrincipal(string userId = "u1")
        => new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }, "TestAuth"));

    [Fact]
    public async Task Index_ReturnsViewWithRoutines_AndSetsTagsInViewBag()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
        };

        routineSvc.Setup(s => s.GetAllRoutinesAsync("u1", It.IsAny<List<int>>()))
                  .ReturnsAsync(new List<RoutineVM> { new RoutineVM { Id = 1, Name = "R1" } });

        routineSvc.Setup(s => s.GetUserTagsAsync("u1"))
                  .ReturnsAsync(new List<TagVM> { new TagVM { Id = 10, Name = "TagA" } });

        var result = await controller.Index(new List<int> { 10 });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<RoutineVM>>(view.Model);
        model.Should().HaveCount(1);

        List<TagVM> tags = Assert.IsType<List<TagVM>>(controller.ViewBag.AvailableTags);
        tags.Count.Should().Be(1);
        tags[0].Id.Should().Be(10);
        tags[0].Name.Should().Be("TagA");

        List<int> selected = Assert.IsType<List<int>>(controller.ViewBag.SelectedTagIds);
        selected.Should().BeEquivalentTo(new List<int> { 10 });
    }

    [Fact]
    public async Task ToggleStep_WhenServiceReturnsTrue_ReturnsOk()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = BuildPrincipal("u2") }
        };

        routineSvc.Setup(s => s.ToggleStepAsync(1, 2, It.IsAny<DateOnly>(), true, null, "u2"))
                  .ReturnsAsync(true);

        var dto = new RoutinesController.ToggleStepDto(1, 2, DateOnly.FromDateTime(DateTime.UtcNow), true, null);

        var result = await controller.ToggleStep(dto);

        result.Should().BeOfType<OkResult>();
    }

    [Fact]
    public async Task ToggleStep_WhenServiceReturnsFalse_ReturnsBadRequest()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = BuildPrincipal("u3") }
        };

        routineSvc.Setup(s => s.ToggleStepAsync(5, 7, It.IsAny<DateOnly>(), false, "x", "u3"))
                  .ReturnsAsync(false);

        var dto = new RoutinesController.ToggleStepDto(5, 7, DateOnly.FromDateTime(DateTime.UtcNow), false, "x");

        var result = await controller.ToggleStep(dto);

        result.Should().BeOfType<BadRequestResult>();
    }

    [Fact]
    public async Task ForDate_BadDateString_FallsBackToToday_AndReturnsJson()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = BuildPrincipal("u4") }
        };

        routineSvc.Setup(s => s.GetForDateAsync(It.IsAny<DateOnly>(), "u4"))
                  .ReturnsAsync(new List<RoutineForDayVM>());

        var result = await controller.ForDate("not-a-date");

        result.Should().BeOfType<JsonResult>();
        routineSvc.Verify(s => s.GetForDateAsync(It.IsAny<DateOnly>(), "u4"), Times.Once);
    }
    
    [Fact]
    public async Task Create_Post_NoSteps_AddsModelError_AndReturnsViewWithTags()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        routineSvc.Setup(s => s.GetUserTagsAsync("u1"))
            .ReturnsAsync(new List<TagVM> { new() { Id = 1, Name = "A" } });

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        var vm = new RoutineVM { Name = "R" }; // stepsJson => null => brak kroków

        var result = await controller.Create(vm, stepsJson: null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.False(controller.ModelState.IsValid);
        controller.ModelState[nameof(vm.Steps)].Errors.Should().NotBeEmpty();
        var tags = Assert.IsType<List<TagVM>>(vm.AvailableTags);
        tags.Should().HaveCount(1);
    }

    [Fact]
    public async Task Edit_Get_WhenRoutineNotFound_ReturnsNotFound()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        routineSvc.Setup(s => s.GetRoutineAsync(123, "u1"))
            .ReturnsAsync((RoutineVM?)null);

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        var result = await controller.Edit(123);
        Assert.IsType<NotFoundResult>(result);
    }
    
    [Fact]
    public async Task SetAll_BadDate_ReturnsBadRequest_AndDoesNotCallService()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        var dto = new RoutinesController.SetAllDto(1, "2025/13/40", true);
        var result = await controller.SetAll(dto);

        Assert.IsType<BadRequestResult>(result);
        routineSvc.Verify(s => s.SetAllStepsAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task ToggleProduct_ValidDate_ParsesAndCallsService_ReturnsOk()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        DateOnly parsed;
        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        routineSvc.Setup(s => s.ToggleStepProductAsync(1, 2, 3, It.IsAny<DateOnly>(), true, "u1"))
            .Callback<int,int,int,DateOnly,bool,string>((_,_,_,d,_,_) => parsed = d)
            .ReturnsAsync(true);

        var dto = new RoutinesController.ToggleProductDto(1, 2, 3, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), true);

        var result = await controller.ToggleProduct(dto);

        Assert.IsType<OkResult>(result);
        routineSvc.Verify(s => s.ToggleStepProductAsync(1, 2, 3, It.IsAny<DateOnly>(), true, "u1"), Times.Once);
    }
    
    [Fact]
    public async Task ToggleProduct_BadDate_ReturnsBadRequest()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        var dto = new RoutinesController.ToggleProductDto(1, 2, 3, "2025/10/23", true); // zły format

        var result = await controller.ToggleProduct(dto);
        Assert.IsType<BadRequestResult>(result);
        routineSvc.Verify(s => s.ToggleStepProductAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<DateOnly>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetAll_Valid_ReturnsOk()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        routineSvc.Setup(s => s.SetAllStepsAsync(1, It.IsAny<DateOnly>(), true, "u9"))
            .ReturnsAsync(true);

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u9") }
            }
        };

        var dto = new RoutinesController.SetAllDto(1, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), true);
        var result = await controller.SetAll(dto);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Complete_Valid_ReturnsOk()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        routineSvc.Setup(s => s.SetRoutineCompletedAsync(7, It.IsAny<DateOnly>(), true, "u11"))
            .ReturnsAsync(true);

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u11") }
            }
        };

        var dto = new RoutinesController.CompleteDto(7, DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd"), true);
        var result = await controller.Complete(dto);
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task Complete_BadDate_ReturnsBadRequest_AndDoesNotCallService()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("uX") }
            }
        };

        var dto = new RoutinesController.CompleteDto(9, "xx-yy-zz", true);
        var result = await controller.Complete(dto);

        Assert.IsType<BadRequestResult>(result);
        routineSvc.Verify(s => s.SetRoutineCompletedAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Never);
    }
    
    [Fact]
    public async Task Create_Post_ValidModel_RedirectsToIndex()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        routineSvc.Setup(s => s.CreateRoutineAsync(It.IsAny<RoutineVM>(), "u1"))
            .ReturnsAsync(1);

        // ZBUDUJ stepsJson dokładnie jak w BuildStepsJsonForEditor:
        var stepsPayload = new[]
        {
            new
            {
                id = 0,
                name = "S",
                minutes = 5,
                desc = (string?)null,
                rrule = (string?)null,
                rotation = new { enabled = false, mode = (string?)null },
                products = Array.Empty<object>()
            }
        };
        var stepsJson = System.Text.Json.JsonSerializer.Serialize(stepsPayload);

        // VM nie musi mieć Steps – i tak zostaną nadpisane z stepsJson
        var vm = new RoutineVM
        {
            Name = "R",
            Color = "#3b82f6",
            Icon = "fa-coffee"
        };

        var result = await controller.Create(vm, stepsJson);

        if (result is ViewResult vr && !controller.ModelState.IsValid)
        {
            foreach (var kv in controller.ModelState)
            {
                foreach (var err in kv.Value.Errors)
                {
                    Console.WriteLine($"MODEL ERROR: Key={kv.Key}, Error={err.ErrorMessage}");
                }
            }
        }

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        routineSvc.Verify(s => s.CreateRoutineAsync(It.IsAny<RoutineVM>(), "u1"), Times.Once);
    }
    
    [Fact]
    public async Task Create_Post_NoSteps_ReturnsViewWithModelError()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        routineSvc.Setup(s => s.GetUserTagsAsync("u1"))
            .ReturnsAsync(new List<TagVM> { new() { Id = 1, Name = "A" } });

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        var vm = new RoutineVM { Name = "R" };
        var result = await controller.Create(vm, "[]");

        var view = Assert.IsType<ViewResult>(result);
        Assert.Same(vm, view.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(vm.Steps)));
    }

    [Fact]
    public async Task DeleteConfirmed_CallsServiceAndRedirectsToIndex()
    {
        var routineSvc = new Mock<IRoutineService>();
        var userMgr = MockUserManager();

        var controller = new RoutinesController(routineSvc.Object, userMgr, Mock.Of<ITagService>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = BuildPrincipal("u1") }
            }
        };

        var result = await controller.DeleteConfirmed(99);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        redirect.ActionName.Should().Be("Index");
        routineSvc.Verify(s => s.DeleteRoutineAsync(99, "u1"), Times.Once);
    }

    
}
