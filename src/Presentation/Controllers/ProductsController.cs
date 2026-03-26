using DeliverySystem.Application.DTOs;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using DeliverySystem.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DeliverySystem.Presentation.Controllers;

/// <summary>
/// API controller for product management (CRUD).
/// Read operations are accessible to any authenticated user; write operations require the admin role.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting(RateLimitOptions.ProductsPolicyName)]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductsController"/> class.
    /// </summary>
    /// <param name="productService">The product service.</param>
    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>
    /// Returns all products ordered by name.
    /// </summary>
    /// <returns>A list of all products.</returns>
    /// <response code="200">Products retrieved successfully.</response>
    /// <response code="401">Authentication required.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var products = await _productService.GetAllAsync(ct);
        return Ok(products);
    }

    /// <summary>
    /// Returns a single product by its identifier.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching product.</returns>
    /// <response code="200">Product retrieved successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="404">Product not found.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productService.GetByIdAsync(id, ct);
        return Ok(product);
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="request">The product creation payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The newly created product.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = await _productService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The identifier of the product to update.</param>
    /// <param name="request">The updated product data.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The updated product.</returns>
    /// <response code="200">Product updated successfully.</response>
    /// <response code="400">Validation errors in the request.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    /// <response code="404">Product not found.</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var product = await _productService.UpdateAsync(id, request, ct);
        return Ok(product);
    }

    /// <summary>
    /// Deletes a product by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the product to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>No content.</returns>
    /// <response code="204">Product deleted successfully.</response>
    /// <response code="401">Authentication required.</response>
    /// <response code="403">Admin role required.</response>
    /// <response code="404">Product not found.</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _productService.DeleteAsync(id, ct);
        return NoContent();
    }
}
