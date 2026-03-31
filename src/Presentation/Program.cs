using System.Text;
using DeliverySystem.Application.Options;
using DeliverySystem.Domain.Constants;
using DeliverySystem.Infrastructure;
using DeliverySystem.Infrastructure.Data;
using DeliverySystem.Presentation.Extensions;
using DeliverySystem.Presentation.Filters;
using DeliverySystem.Presentation.Middlewares;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApiConfiguration();

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddValidatorsFromAssemblyContaining<DeliverySystem.Application.Validators.RegisterRequestValidator>();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ValidationFilter>();
    });

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AppRoles.DefaultPolicy, policy =>
        policy.RequireRole(AppRoles.Admin, AppRoles.User));
});

builder.Services.AddAuthRateLimiter(builder.Configuration);

builder.Services
    .AddOptions<CorsOptions>()
    .BindConfiguration(CorsOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

var corsOption = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>()!;

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsOptions.AuthPolicyName, policy =>
    {
        policy.WithOrigins(corsOption.AuthAllowedOrigins)
              .WithMethods(corsOption.AuthAllowedMethods)
              .AllowAnyHeader();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Apply pending EF Core migrations automatically in development
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.Migrate();

    // Seed roles and admin user
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();

    // Enable OpenAPI/Swagger only in development
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "API v1");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRateLimiter();
app.UseCors(CorsOptions.AuthPolicyName);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
