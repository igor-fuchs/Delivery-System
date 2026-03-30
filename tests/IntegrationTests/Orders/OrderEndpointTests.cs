using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliverySystem.IntegrationTests.Infrastructure;
using DeliverySystem.Presentation.Filters;

namespace DeliverySystem.IntegrationTests.Orders;

/// <summary>
/// End-to-end tests for the <c>/api/orders</c> endpoints.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class OrderEndpointTests : IntegrationTestBase
{
    public OrderEndpointTests(DeliverySystemFactory factory) : base(factory) { }

    #region POST /api/orders

    [Fact]
    public async Task Create_AuthenticatedUser_ValidRequest_ReturnsCreated()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);

        var userToken = await GetTokenAsync($"user-order-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "My first order",
                Items = new[] { new { ProductId = product!.Id, Quantity = 2 } }
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        var response = await userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("Pending", body.Status);
    }

    [Fact]
    public async Task Create_EmptyItems_ReturnsBadRequest()
    {
        var userToken = await GetTokenAsync($"user-empty-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "Order with no items",
                Items = Array.Empty<object>()
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        var response = await userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_EmptyDescription_ReturnsBadRequest()
    {
        var userToken = await GetTokenAsync($"user-nodesc-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "",
                Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 1 } }
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        var response = await userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingProduct_ReturnsNotFound()
    {
        var userToken = await GetTokenAsync($"user-missing-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "Order for non-existent product",
                Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 1 } }
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        var response = await userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_UnavailableProduct_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient, stock: false);

        var userToken = await GetTokenAsync($"user-unavail-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "Order for unavailable product",
                Items = new[] { new { ProductId = product!.Id, Quantity = 1 } }
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        var response = await userClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Unauthenticated_ReturnsUnauthorized()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "Desc",
                Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 1 } }
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region GET /api/orders

    [Fact]
    public async Task GetAll_Admin_ReturnsAllOrders()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);

        // Create orders for two different users
        var user1Token = await GetTokenAsync($"user-getall1-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var user1Client = CreateAuthenticatedClient(user1Token);
        await CreateOrderAsync(user1Client, product!.Id);

        var user2Token = await GetTokenAsync($"user-getall2-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var user2Client = CreateAuthenticatedClient(user2Token);
        await CreateOrderAsync(user2Client, product.Id);

        var response = await adminClient.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<OrderResponseDto[]>();
        // Admin sees at least 2 orders (may be more from other tests)
        Assert.True(orders!.Length >= 2);
    }

    [Fact]
    public async Task GetAll_RegularUser_ReturnsOnlyOwnOrders()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);

        var user1Token = await GetTokenAsync($"user-own1-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var user1Client = CreateAuthenticatedClient(user1Token);
        var order = await CreateOrderAsync(user1Client, product!.Id);

        var user2Token = await GetTokenAsync($"user-own2-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var user2Client = CreateAuthenticatedClient(user2Token);
        await CreateOrderAsync(user2Client, product.Id);

        var response = await user1Client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var orders = await response.Content.ReadFromJsonAsync<OrderResponseDto[]>();
        Assert.All(orders!, o => Assert.Equal(order!.CustomerId, o.CustomerId));
    }

    #endregion

    #region GET /api/orders/{id}

    [Fact]
    public async Task GetById_OwnOrder_ReturnsOk()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);

        var userToken = await GetTokenAsync($"user-ownorder-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);
        var order = await CreateOrderAsync(userClient, product!.Id);

        var response = await userClient.GetAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var userToken = await GetTokenAsync($"user-notfound-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var response = await userClient.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region PUT /api/orders/{id}

    [Fact]
    public async Task UpdateStatus_Admin_ValidStatus_ReturnsOk()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);
        var order = await CreateOrderAsync(adminClient, product!.Id);

        var response = await adminClient.PutAsJsonAsync($"/api/orders/{order!.Id}", new
        {
            Status = "Shipped"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
        Assert.Equal("Shipped", body!.Status);
        Assert.NotNull(body.UpdatedAt);
    }

    [Fact]
    public async Task UpdateStatus_Admin_InvalidStatus_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);
        var order = await CreateOrderAsync(adminClient, product!.Id);

        var response = await adminClient.PutAsJsonAsync($"/api/orders/{order!.Id}", new
        {
            Status = "InvalidStatus"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_RegularUser_ReturnsForbidden()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);
        var order = await CreateOrderAsync(adminClient, product!.Id);

        var userToken = await GetTokenAsync($"user-putstatus-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var response = await userClient.PutAsJsonAsync($"/api/orders/{order!.Id}", new
        {
            Status = "Shipped"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region DELETE /api/orders/{id}

    [Fact]
    public async Task Delete_Admin_ExistingOrder_ReturnsNoContent()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);
        var order = await CreateOrderAsync(adminClient, product!.Id);

        var response = await adminClient.DeleteAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RegularUser_ReturnsForbidden()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var product = await CreateProductAsync(adminClient);
        var order = await CreateOrderAsync(adminClient, product!.Id);

        var userToken = await GetTokenAsync($"user-del-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var response = await userClient.DeleteAsync($"/api/orders/{order!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Admin_UnknownId_ReturnsNotFound()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);

        var response = await adminClient.DeleteAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    private async Task<ProductResponseDto?> CreateProductAsync(HttpClient client, bool stock = true)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Name = $"Product-{Guid.NewGuid()}",
            Description = "Test product.",
            Stock = stock,
            Price = 10.00
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductResponseDto>();
    }

    private async Task<OrderResponseDto?> CreateOrderAsync(HttpClient client, Guid productId)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
        {
            Content = JsonContent.Create(new
            {
                Description = "Test order",
                Items = new[] { new { ProductId = productId, Quantity = 1 } }
            })
        };
        request.Headers.Add(IdempotencyFilter.HeaderName, Guid.NewGuid().ToString());

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<OrderResponseDto>();
    }

    private sealed record ProductResponseDto(Guid Id, string Name, string Description, bool Stock, decimal Price, DateTime CreatedAt);

    private sealed record OrderResponseDto(Guid Id, Guid CustomerId, string Description, string Status, decimal TotalAmount, DateTime CreatedAt, DateTime? UpdatedAt, object[]? Items);
}
