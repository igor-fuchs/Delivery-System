using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using DeliverySystem.IntegrationTests.Infrastructure;

namespace DeliverySystem.IntegrationTests.Products;

/// <summary>
/// End-to-end tests for the <c>/api/products</c> endpoints.
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class ProductEndpointTests : IntegrationTestBase
{
    public ProductEndpointTests(DeliverySystemFactory factory) : base(factory) { }

    #region GET /api/products

    [Fact]
    public async Task GetAll_Unauthenticated_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_AuthenticatedUser_ReturnsOk()
    {
        var token = await GetTokenAsync($"user-getall-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync("/api/products");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region GET /api/products/{id}

    [Fact]
    public async Task GetById_ExistingProduct_ReturnsOk()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var created = await CreateProductAsync(client);

        var response = await client.GetAsync($"/api/products/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal(created.Id, body!.Id);
    }

    [Fact]
    public async Task GetById_UnknownId_ReturnsNotFound()
    {
        var token = await GetTokenAsync($"user-getbyid-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var client = CreateAuthenticatedClient(token);

        var response = await client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region POST /api/products

    [Fact]
    public async Task Create_Admin_ValidRequest_ReturnsCreated()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Name = $"Product-{Guid.NewGuid()}",
            Description = "Integration test product.",
            Stock = true,
            Price = 19.99
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.NotNull(body?.Id);
    }

    [Fact]
    public async Task Create_RegularUser_ReturnsForbidden()
    {
        var token = await GetTokenAsync($"user-create-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var client = CreateAuthenticatedClient(token);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Name = "Widget",
            Description = "Desc.",
            Stock = true,
            Price = 10
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Create_Admin_InvalidRequest_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Name = "",        // invalid
            Description = "Desc.",
            Stock = true,
            Price = 10
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Admin_MissingDescription_ReturnsBadRequest()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Name = "Widget",
            Description = "",  // invalid — required
            Stock = true,
            Price = 10
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region PUT /api/products/{id}

    [Fact]
    public async Task Update_Admin_ValidRequest_ReturnsOk()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var created = await CreateProductAsync(client);

        var response = await client.PutAsJsonAsync($"/api/products/{created!.Id}", new
        {
            Name = "Updated Name",
            Description = "Updated description.",
            Stock = false,
            Price = 99.99
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
        Assert.Equal("Updated Name", body!.Name);
        Assert.False(body.Stock);
    }

    [Fact]
    public async Task Update_RegularUser_ReturnsForbidden()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var created = await CreateProductAsync(adminClient);

        var userToken = await GetTokenAsync($"user-put-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var response = await userClient.PutAsJsonAsync($"/api/products/{created!.Id}", new
        {
            Name = "Hack",
            Description = "Desc.",
            Stock = true,
            Price = 1
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Update_Admin_UnknownId_ReturnsNotFound()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var response = await client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", new
        {
            Name = "Name",
            Description = "Desc.",
            Stock = true,
            Price = 1
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region DELETE /api/products/{id}

    [Fact]
    public async Task Delete_Admin_ExistingProduct_ReturnsNoContent()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var created = await CreateProductAsync(client);

        var response = await client.DeleteAsync($"/api/products/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_RegularUser_ReturnsForbidden()
    {
        var adminToken = await GetAdminTokenAsync();
        var adminClient = CreateAuthenticatedClient(adminToken);
        var created = await CreateProductAsync(adminClient);

        var userToken = await GetTokenAsync($"user-delete-{Guid.NewGuid()}@test.com", "P@ssw0rd!");
        var userClient = CreateAuthenticatedClient(userToken);

        var response = await userClient.DeleteAsync($"/api/products/{created!.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Admin_UnknownId_ReturnsNotFound()
    {
        var adminToken = await GetAdminTokenAsync();
        var client = CreateAuthenticatedClient(adminToken);

        var response = await client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    private async Task<ProductResponseDto?> CreateProductAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/products", new
        {
            Name = $"Test-Product-{Guid.NewGuid()}",
            Description = "Test description.",
            Stock = true,
            Price = 9.99
        });
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductResponseDto>();
    }

    private sealed record ProductResponseDto(Guid Id, string Name, string Description, bool Stock, decimal Price, DateTime CreatedAt);
}
