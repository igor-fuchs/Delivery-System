using DeliverySystem.Application.Options;
using DeliverySystem.Domain.Constants;
using DeliverySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DeliverySystem.Infrastructure.Data;

/// <summary>
/// Seeds the database with the required roles and the initial admin user.
/// Intended to run once at application startup.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly AdminSeedOptions _adminOptions;
    private readonly ILogger<DatabaseSeeder> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
    /// </summary>
    /// <param name="roleManager">The Identity role manager.</param>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="adminOptions">The admin seed user configuration.</param>
    /// <param name="logger">Logger for seed-related events.</param>
    public DatabaseSeeder(
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<ApplicationUser> userManager,
        IOptions<AdminSeedOptions> adminOptions,
        ILogger<DatabaseSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _adminOptions = adminOptions.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates the application roles and the admin seed user if they do not already exist.
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = [AppRoles.Admin, AppRoles.User];

        foreach (var role in roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid> { Name = role });
                _logger.LogInformation("Created role '{Role}'", role);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var existingUser = await _userManager.FindByEmailAsync(_adminOptions.Email);

        if (existingUser is not null)
        {
            // Ensure existing user has the admin role
            if (!await _userManager.IsInRoleAsync(existingUser, AppRoles.Admin))
            {
                await _userManager.AddToRoleAsync(existingUser, AppRoles.Admin);
                _logger.LogInformation("Assigned '{Role}' role to existing admin user", AppRoles.Admin);
            }

            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = _adminOptions.Email,
            Email = _adminOptions.Email,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(adminUser, _adminOptions.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to create admin seed user: {Errors}", errors);
            return;
        }

        await _userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        _logger.LogInformation("Admin seed user created successfully");
    }
}
