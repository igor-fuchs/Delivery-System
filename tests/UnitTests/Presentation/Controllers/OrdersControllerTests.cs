using System.Security.Claims;
using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Domain.Constants;
using DeliverySystem.Presentation.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DeliverySystem.UnitTests.Presentation.Controllers;

public sealed class OrdersControllerTests
{
    private readonly IOrderService _orderService = Substitute.For<IOrderService>();
    private readonly OrdersController _sut;

    private static readonly Guid UserId = Guid.NewGuid();

    public OrdersControllerTests()
    {
        _sut = new OrdersController(_orderService);
    }

    private void SetUser(bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, UserId.ToString()),
            new(ClaimTypes.Role, isAdmin ? AppRoles.Admin : AppRoles.User)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    private static OrderResponse MakeResponse(Guid? customerId = null) => new(
        Guid.NewGuid(),
        customerId ?? UserId,
        "Test order",
        "Pending",
        10m,
        DateTime.UtcNow,
        null,
        []);

    #region GetAll

    [Fact]
    public async Task GetAll_AdminUser_PassesNullCustomerIdToService()
    {
        SetUser(isAdmin: true);
        _orderService.GetAllAsync(null, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.GetAll(CancellationToken.None);

        await _orderService.Received(1).GetAllAsync(null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_RegularUser_PassesUserIdToService()
    {
        SetUser(isAdmin: false);
        _orderService.GetAllAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);

        await _sut.GetAll(CancellationToken.None);

        await _orderService.Received(1).GetAllAsync(UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAll_Success_ReturnsOkWithList()
    {
        SetUser(isAdmin: true);
        var expected = (IReadOnlyList<OrderResponse>)[MakeResponse()];
        _orderService.GetAllAsync(null, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetById_AdminUser_PassesNullRequesterIdToService()
    {
        SetUser(isAdmin: true);
        var response = MakeResponse(customerId: Guid.NewGuid());
        _orderService.GetByIdAsync(response.Id, null, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.GetById(response.Id, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        await _orderService.Received(1).GetByIdAsync(response.Id, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_RegularUser_PassesUserIdAsRequesterToService()
    {
        SetUser(isAdmin: false);
        var response = MakeResponse(customerId: UserId);
        _orderService.GetByIdAsync(response.Id, UserId, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.GetById(response.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
        await _orderService.Received(1).GetByIdAsync(response.Id, UserId, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_Success_ReturnsCreatedAtAction()
    {
        SetUser(isAdmin: false);
        var response = MakeResponse();
        var request = new CreateOrderRequest("Desc", [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);
        _orderService.CreateAsync(UserId, request, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(OrdersController.GetById), created.ActionName);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task Create_ShouldPassCurrentUserIdToService()
    {
        SetUser(isAdmin: false);
        var request = new CreateOrderRequest("Desc", [new CreateOrderItemRequest(Guid.NewGuid(), 1)]);
        _orderService.CreateAsync(UserId, request, Arg.Any<CancellationToken>()).Returns(MakeResponse());

        await _sut.Create(request, CancellationToken.None);

        await _orderService.Received(1).CreateAsync(UserId, request, Arg.Any<CancellationToken>());
    }

    #endregion

    #region UpdateStatus

    [Fact]
    public async Task UpdateStatus_Success_ReturnsOk()
    {
        SetUser(isAdmin: true);
        var id = Guid.NewGuid();
        var response = MakeResponse();
        var request = new UpdateOrderStatusRequest("Shipped");
        _orderService.UpdateStatusAsync(id, request, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.UpdateStatus(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_Success_ReturnsNoContent()
    {
        SetUser(isAdmin: true);
        var id = Guid.NewGuid();
        _orderService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldDelegateToService()
    {
        SetUser(isAdmin: true);
        var id = Guid.NewGuid();
        _orderService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _sut.Delete(id, CancellationToken.None);

        await _orderService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion
}
