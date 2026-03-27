using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DeliverySystem.Presentation.Extensions;

/// <summary>
/// Extension methods for configuring OpenAPI/Swagger document generation
/// with metadata, security schemes, and tag descriptions optimized for Postman import.
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Registers OpenAPI services with full API metadata, JWT Bearer security scheme,
    /// server definitions, and tag descriptions.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddOpenApiConfiguration(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Delivery System API",
                    Version = "v1",
                    Description = """
                        RESTful API for a delivery system that manages user authentication, product catalog, and order lifecycle.

                        ## Authentication
                        All endpoints (except `/api/auth/*`) require a **JWT Bearer token** in the `Authorization` header.
                        Obtain a token via `POST /api/auth/register`, `POST /api/auth/login`, or `POST /api/auth/google`.

                        ## Roles
                        - **user** — Can browse products and manage their own orders.
                        - **admin** — Full access: manage products, view/update/delete any order.

                        ## Rate Limiting
                        All endpoints are rate-limited per IP address. Auth endpoints have stricter limits.
                        When the limit is exceeded, the API returns `429 Too Many Requests`.

                        ## Idempotency
                        The `POST /api/orders` endpoint requires an `Idempotency-Key` header (max 64 characters).
                        Repeated requests with the same key within 60 minutes return the cached original response.

                        ## Error Responses
                        All errors follow a consistent JSON structure with a machine-readable `errorCode` for i18n:
                        ```json
                        {
                          "message": "Human-readable description",
                          "errorCode": "MACHINE_READABLE_CODE",
                          "errors": {
                            "fieldName": [{ "code": "FIELD_CODE", "message": "..." }]
                          }
                        }
                        ```
                        The `errors` field is only present for validation errors (`errorCode: "VALIDATION_FAILED"`).
                        """,
                    Contact = new OpenApiContact
                    {
                        Name = "Delivery System Team"
                    },
                    License = new OpenApiLicense
                    {
                        Name = "MIT"
                    }
                };

                document.Servers =
                [
                    new OpenApiServer
                    {
                        Url = "http://localhost:5000",
                        Description = "Local Development (Docker Compose)"
                    },
                    new OpenApiServer
                    {
                        Url = "https://localhost:5001",
                        Description = "Local Development (HTTPS)"
                    }
                ];

                document.Tags = new HashSet<OpenApiTag>
                {
                    new OpenApiTag
                    {
                        Name = "Auth",
                        Description = "User registration, login (email/password and Google OAuth2), and JWT token issuance. These endpoints are public and do not require authentication. Rate-limited per IP."
                    },
                    new OpenApiTag
                    {
                        Name = "Products",
                        Description = "Product catalog management (CRUD). Read operations require authentication; write operations (create, update, delete) require the **admin** role."
                    },
                    new OpenApiTag
                    {
                        Name = "Orders",
                        Description = "Order lifecycle management. Authenticated users can create orders and view their own; admins can view, update status, and delete any order. Order creation requires an `Idempotency-Key` header."
                    }
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["Bearer"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = JwtBearerDefaults.AuthenticationScheme.ToLowerInvariant(),
                        BearerFormat = "JWT",
                        Description = "Enter the JWT token obtained from the auth endpoints. Example: `eyJhbGciOi...`"
                    }
                };

                return Task.CompletedTask;
            });

            options.AddOperationTransformer((operation, context, _) =>
            {
                var metadata = context.Description.ActionDescriptor.EndpointMetadata;

                // Apply global security requirement to endpoints that have [Authorize]
                var hasAuthorize = metadata.OfType<Microsoft.AspNetCore.Authorization.AuthorizeAttribute>().Any();
                var hasAllowAnonymous = metadata.OfType<Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute>().Any();

                if (hasAuthorize && !hasAllowAnonymous)
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer")] = new List<string>()
                        }
                    ];
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
