using DeliverySystem.Application.Interfaces;
using Ganss.Xss;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Strips all HTML tags from user input using the Ganss.Xss HtmlSanitizer library,
/// configured with empty allowlists to produce plain text output.
/// </summary>
public sealed class CleanerService : ICleanerService
{
    private readonly HtmlSanitizer _sanitizer;

    /// <summary>
    /// Initializes the sanitizer with empty allowlists so that all HTML tags,
    /// attributes, CSS properties, and URI schemes are removed.
    /// </summary>
    public CleanerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.KeepChildNodes = false;
    }

    /// <inheritdoc />
    public string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = _sanitizer.Sanitize(input);
        return sanitized.Trim();
    }
}
