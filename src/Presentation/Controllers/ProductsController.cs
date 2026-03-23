using DeliverySystem.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliverySystem.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private static readonly List<ProductDto> _products = new();

    [HttpGet]
    [Authorize(Policy = AppRoles.DefaultPolicy)]

    public IActionResult GetAll()
    {
        return Ok(_products);
    }
}

public class ProductDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
}