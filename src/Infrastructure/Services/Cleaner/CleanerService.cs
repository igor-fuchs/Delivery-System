using DeliverySystem.Application.Interfaces;
using Ganss.Xss;
using System.Text;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Cleans and normalizes user input using the Ganss.Xss HtmlSanitizer library.
/// All HTML tags and their inner content are stripped; whitespace is trimmed;
/// and Unicode characters are normalized to Form C (Composed) to produce
/// consistent plain-text values safe for database storage.
/// </summary>
public sealed class CleanerService : ICleanerService
{
    private readonly HtmlSanitizer _sanitizer;

    /// <summary>
    /// Initializes the sanitizer with empty allowlists so that all HTML tags,
    /// attributes, CSS properties, and URI schemes — including their inner content —
    /// are fully removed.
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

        return _sanitizer.Sanitize(input).Trim();
    }

    /// <inheritdoc />
    public string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input.Normalize(NormalizationForm.FormC).Trim();
    }

    /// <inheritdoc />
    public string Clean(string? input)
    {
        var sanitized = Sanitize(input);
        return Normalize(sanitized);
    }
}
