using System.Net.Http.Json;
using System.Text.Json.Serialization;
using DeliverySystem.Application.Interfaces;
using DeliverySystem.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DeliverySystem.Application.Exceptions;

namespace DeliverySystem.Infrastructure.Services;

/// <summary>
/// Google reCAPTCHA implementation of <see cref="ICaptchaService"/>.
/// Verifies tokens against the Google <c>siteverify</c> API.
/// </summary>
public sealed class RecaptchaService : ICaptchaService
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaOptions _options;
    private readonly ILogger<RecaptchaService> _logger;

    public RecaptchaService(
        HttpClient httpClient,
        IOptions<RecaptchaOptions> options,
        ILogger<RecaptchaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    #region Implementation

    /// <inheritdoc />
    public async Task<bool> ValidateAsync(string token, CancellationToken cancellationToken = default)
    {
        var response = await SendVerificationRequestAsync(token, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("reCAPTCHA request failed with status {StatusCode}", response.StatusCode);
            ThrowValidationException();
        }

        var result = await response.Content.ReadFromJsonAsync<RecaptchaResponse>(cancellationToken);

        if (result is null || !result.Success)
        {
            _logger.LogWarning("reCAPTCHA verification failed. Error codes: {ErrorCodes}",
                string.Join(", ", result?.ErrorCodes ?? Array.Empty<string>()));
            ThrowValidationException();
        }

        if (result!.Score.HasValue && result.Score.Value < _options.MinimumScore)
        {
            _logger.LogWarning("reCAPTCHA score {Score} is below minimum {MinimumScore}",
                result.Score.Value, _options.MinimumScore);
            ThrowValidationException();
        }

        return true;
    }
    #endregion

    #region Private Helpers

    private async Task<HttpResponseMessage> SendVerificationRequestAsync(string token, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["secret"] = _options.SecretKey,
            ["response"] = token
        };

        using var content = new FormUrlEncodedContent(parameters);
        return await _httpClient.PostAsync("siteverify", content, cancellationToken);
    }

    private static void ThrowValidationException()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["captchaToken"] = ["Invalid or expired CAPTCHA token."]
        };

        throw new ValidationException(errors);
    }
    #endregion

    #region Private DTOs

    private sealed class RecaptchaResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }

        [JsonPropertyName("score")]
        public double? Score { get; init; }

        [JsonPropertyName("action")]
        public string? Action { get; init; }

        [JsonPropertyName("challenge_ts")]
        public string? ChallengeTimestamp { get; init; }

        [JsonPropertyName("hostname")]
        public string? Hostname { get; init; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; init; }
    }
    #endregion
}