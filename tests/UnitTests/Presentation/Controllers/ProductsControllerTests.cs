using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Presentation.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace DeliverySystem.UnitTests.Presentation.Controllers;

public sealed class ProductsControllerTests
{
    private readonly IProductService _productService = Substitute.For<IProductService>();
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_productService);
    }

    private static ProductResponse MakeResponse() => new(
        Guid.NewGuid(), "Widget", "Desc", true, 9.99m, DateTime.UtcNow);

    #region GetAll

    [Fact]
    public async Task GetAll_Success_ReturnsOkWithList()
    {
        var expected = (IReadOnlyList<ProductResponse>)[MakeResponse(), MakeResponse()];
        _productService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetAll_ShouldDelegateToService()
    {
        _productService.GetAllAsync(Arg.Any<CancellationToken>()).Returns([]);

        await _sut.GetAll(CancellationToken.None);

        await _productService.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region GetById

    [Fact]
    public async Task GetById_ExistingId_ReturnsOkWithProduct()
    {
        var expected = MakeResponse();
        _productService.GetByIdAsync(expected.Id, Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sut.GetById(expected.Id, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expected, ok.Value);
    }

    [Fact]
    public async Task GetById_ShouldDelegateToService()
    {
        var id = Guid.NewGuid();
        _productService.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(MakeResponse());

        await _sut.GetById(id, CancellationToken.None);

        await _productService.Received(1).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_Success_ReturnsCreatedAtAction()
    {
        var response = MakeResponse();
        var request = new CreateProductRequest("Widget", "Desc", true, 9.99m);
        _productService.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Create(request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProductsController.GetById), created.ActionName);
        Assert.Equal(response, created.Value);
    }

    [Fact]
    public async Task Create_ShouldDelegateToService()
    {
        var request = new CreateProductRequest("Widget", "Desc", true, 9.99m);
        _productService.CreateAsync(request, Arg.Any<CancellationToken>()).Returns(MakeResponse());

        await _sut.Create(request, CancellationToken.None);

        await _productService.Received(1).CreateAsync(request, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_Success_ReturnsOkWithUpdatedProduct()
    {
        var id = Guid.NewGuid();
        var response = MakeResponse();
        var request = new UpdateProductRequest("Updated", "Desc", false, 5m);
        _productService.UpdateAsync(id, request, Arg.Any<CancellationToken>()).Returns(response);

        var result = await _sut.Update(id, request, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, ok.Value);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_Success_ReturnsNoContent()
    {
        var id = Guid.NewGuid();
        _productService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var result = await _sut.Delete(id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ShouldDelegateToService()
    {
        var id = Guid.NewGuid();
        _productService.DeleteAsync(id, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await _sut.Delete(id, CancellationToken.None);

        await _productService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    #endregion
}
