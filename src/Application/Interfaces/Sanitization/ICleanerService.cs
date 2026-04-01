namespace DeliverySystem.Application.Interfaces;

/// <summary>
/// Cleans and normalizes untrusted user input to prevent stored XSS attacks
/// and ensure consistent plain-text storage.
/// </summary>
public interface ICleanerService
{
    /// <summary>
    /// Removes all HTML tags and their content from the input string, then trims
    /// leading/trailing whitespace. Returns an empty string when <paramref name="input"/>
    /// is <c>null</c> or whitespace-only.
    /// </summary>
    /// <param name="input">The untrusted user input that may contain HTML markup.</param>
    /// <returns>The plain-text content with all HTML tags and their inner content removed.</returns>
    string Sanitize(string? input);

    /// <summary>
    /// Normalizes the input string using Unicode Normalization Form C (Composed) and trims
    /// leading/trailing whitespace. This decomposes and recomposes Unicode characters to ensure
    /// consistent representation of accented characters and special symbols (e.g., é is normalized
    /// to a single character instead of e + combining accent).
    /// Returns an empty string when <paramref name="input"/> is <c>null</c> or whitespace-only.
    /// Does not strip HTML — call <see cref="Sanitize"/> or <see cref="Clean"/> when
    /// HTML removal is also required.
    /// </summary>
    /// <param name="input">The plain-text input to normalize.</param>
    /// <returns>The trimmed, Unicode-normalized (NFC) string.</returns>
    string Normalize(string? input);

    /// <summary>
    /// Applies the full cleaning pipeline: first strips all HTML tags and their content
    /// via <see cref="Sanitize"/>, then applies Unicode normalization via <see cref="Normalize"/>.
    /// Sanitize trims leading/trailing whitespace and returns empty string for null/whitespace-only input.
    /// Normalize applies Unicode Normalization Form C (NFC) to ensure consistent representation
    /// of accented and special characters, then trims the result.
    /// Use this method for all free-text user input before persisting to the database.
    /// </summary>
    /// <param name="input">The untrusted user input that may contain HTML markup.</param>
    /// <returns>The sanitized, trimmed, and Unicode-normalized plain-text string.</returns>
    string Clean(string? input);
}
