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
    /// Normalizes whitespace in the input string by trimming leading/trailing whitespace
    /// and collapsing consecutive whitespace characters (spaces, tabs, newlines) into a
    /// single space. Returns an empty string when <paramref name="input"/> is <c>null</c>
    /// or whitespace-only. Does not strip HTML — call <see cref="Sanitize"/> or
    /// <see cref="Clean"/> when HTML removal is also required.
    /// </summary>
    /// <param name="input">The plain-text input to normalize.</param>
    /// <returns>The whitespace-normalized string.</returns>
    string Normalize(string? input);

    /// <summary>
    /// Applies the full cleaning pipeline: first strips all HTML tags and their content
    /// via <see cref="Sanitize"/>, then normalizes whitespace via <see cref="Normalize"/>.
    /// Returns an empty string when <paramref name="input"/> is <c>null</c> or whitespace-only.
    /// Use this method for all free-text user input before persisting to the database.
    /// </summary>
    /// <param name="input">The untrusted user input that may contain HTML markup.</param>
    /// <returns>The sanitized and whitespace-normalized plain-text string.</returns>
    string Clean(string? input);
}
