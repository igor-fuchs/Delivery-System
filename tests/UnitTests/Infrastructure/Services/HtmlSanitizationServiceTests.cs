using DeliverySystem.Infrastructure.Services;

namespace DeliverySystem.UnitTests.Infrastructure.Services;

/// <summary>
/// Unit tests for <see cref="CleanerService"/>.
/// </summary>
public sealed class CleanerServiceTests
{
    private readonly CleanerService _sut = new();

    #region Sanitize

    [Fact]
    public void Sanitize_WithNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Sanitize(null));
    }

    [Fact]
    public void Sanitize_WithEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Sanitize(string.Empty));
    }

    [Fact]
    public void Sanitize_WithWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Sanitize("   "));
    }

    [Fact]
    public void Sanitize_WithPlainText_ReturnsTrimmedText()
    {
        Assert.Equal("Hello World", _sut.Sanitize("  Hello World  "));
    }

    [Fact]
    public void Sanitize_WithScriptTag_StripsTagAndContent()
    {
        var result = _sut.Sanitize("<script>alert('xss')</script>");

        Assert.DoesNotContain("<script>", result);
        Assert.DoesNotContain("</script>", result);
    }

    [Fact]
    public void Sanitize_WithHtmlTags_StripsTagsAndContent()
    {
        // KeepChildNodes = false — inner content is removed along with the tags
        var result = _sut.Sanitize("<b>Bold</b> plain");

        Assert.DoesNotContain("<b>", result);
        Assert.DoesNotContain("Bold", result);
        Assert.Contains("plain", result);
    }

    [Fact]
    public void Sanitize_WithImgTag_StripsTag()
    {
        var result = _sut.Sanitize("<img src=x onerror=alert('xss')>Safe text");

        Assert.DoesNotContain("<img", result);
        Assert.Contains("Safe text", result);
    }

    #endregion

    #region Normalize

    [Fact]
    public void Normalize_WithNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Normalize(null));
    }

    [Fact]
    public void Normalize_WithEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Normalize(string.Empty));
    }

    [Fact]
    public void Normalize_WithWhitespace_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Normalize("   "));
    }

    [Fact]
    public void Normalize_TrimsAndNormalizesUnicode()
    {
        // Normalize trims leading/trailing whitespace and applies Unicode normalization
        Assert.Equal("Hello", _sut.Normalize("   Hello   "));
    }

    [Fact]
    public void Normalize_PresservesInternalWhitespace()
    {
        // Unicode normalization does not collapse multiple spaces
        Assert.Equal("Hello   World", _sut.Normalize("Hello   World"));
    }

    [Fact]
    public void Normalize_NormalizesAccentedCharacters()
    {
        // NFC normalization composes accented characters consistently
        var input = "e\u0301"; // e + combining acute accent (decomposed form)
        var result = _sut.Normalize(input);
        var expected = "é"; // composed form

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Normalize_DoesNotStripHtml()
    {
        // Normalize only performs Unicode normalization — HTML is left intact
        var result = _sut.Normalize("<b>Bold</b>");

        Assert.Contains("<b>", result);
    }

    #endregion

    #region Clean

    [Fact]
    public void Clean_WithNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Clean(null));
    }

    [Fact]
    public void Clean_WithEmpty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, _sut.Clean(string.Empty));
    }

    [Fact]
    public void Clean_StripsHtmlAndNormalizesUnicode()
    {
        // Sanitize removes HTML ("  Hello      World  ") and trims → "Hello      World"
        // Normalize applies Unicode normalization (no change) and trims (already trimmed) → "Hello      World"
        var result = _sut.Clean("  Hello   <b>ignored</b>   World  ");

        Assert.DoesNotContain("<b>", result);
        Assert.Equal("Hello      World", result);
    }

    [Fact]
    public void Clean_WithScriptTag_StripsTagAndContent()
    {
        var result = _sut.Clean("<script>alert('xss')</script>Safe text");

        Assert.DoesNotContain("<script>", result);
        Assert.Contains("Safe text", result);
    }

    [Fact]
    public void Clean_WithPlainTextAndExtraSpaces_TrimsAndNormalizesUnicode()
    {
        // Sanitize trims leading/trailing → "Hello   World"
        // Normalize applies Unicode normalization and trims again → "Hello   World" (already trimmed)
        Assert.Equal("Hello   World", _sut.Clean("  Hello   World  "));
    }

    #endregion
}
