// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace EricksonLopez.Concurrency.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods on <see cref="HttpRequest"/> and <see cref="HttpResponse"/> for managing concurrency ETags and precondition headers.
/// </summary>
public static class ConcurrencyHttpExtensions
{
    /// <summary>
    /// Extracts the expected concurrency token from the incoming HTTP request's <c>If-Match</c> or <c>If-None-Match</c> headers.
    /// </summary>
    /// <param name="request">The incoming HTTP request.</param>
    /// <returns>The extracted <see cref="ConcurrencyToken"/>, or <see langword="null"/> if no matching ETag header was supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/></exception>
    public static ConcurrencyToken? GetExpectedConcurrencyToken(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? rawHeader = request.Headers.IfMatch.ToString();
        if (string.IsNullOrWhiteSpace(rawHeader))
        {
            rawHeader = request.Headers.IfNoneMatch.ToString();
        }

        string sanitized = NormalizeETagValue(rawHeader);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        return ConcurrencyToken.From(sanitized);
    }

    /// <summary>
    /// Extracts the expected numeric concurrency version from the incoming HTTP request's <c>If-Match</c> or <c>If-None-Match</c> headers.
    /// </summary>
    /// <param name="request">The incoming HTTP request.</param>
    /// <returns>The extracted <see cref="ExpectedVersion"/> constraint, or <see langword="null"/> if no numeric version ETag header was supplied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/></exception>
    public static ExpectedVersion? GetExpectedConcurrencyVersion(this HttpRequest request)
    {
        ConcurrencyToken? token = request.GetExpectedConcurrencyToken();
        if (token is null || string.IsNullOrWhiteSpace(token.Value.Value))
        {
            return null;
        }

        if (ConcurrencyVersion.TryParse(token.Value.Value, out ConcurrencyVersion version))
        {
            return ExpectedVersion.Specific(version.Value);
        }

        return null;
    }

    /// <summary>
    /// Sets the HTTP <c>ETag</c> response header formatted with the specified <see cref="IConcurrencyToken"/>.
    /// </summary>
    /// <param name="response">The outgoing HTTP response.</param>
    /// <param name="token">The concurrency token.</param>
    /// <param name="isWeak">A value indicating whether to emit a weak validator (<c>W/"..."</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> or <paramref name="token"/> is <see langword="null"/></exception>
    public static void SetConcurrencyETag(this HttpResponse response, IConcurrencyToken token, bool isWeak = true)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(token);

        if (token.IsEmpty)
        {
            return;
        }

        string rawValue = token.Value;
        string etag = isWeak ? $"W/\"{rawValue}\"" : $"\"{rawValue}\"";
        response.Headers[HeaderNames.ETag] = etag;
    }

    /// <summary>
    /// Sets the HTTP <c>ETag</c> response header formatted with the specified <see cref="ConcurrencyToken"/>.
    /// </summary>
    /// <param name="response">The outgoing HTTP response.</param>
    /// <param name="token">The concurrency token value struct.</param>
    /// <param name="isWeak">A value indicating whether to emit a weak validator (<c>W/"..."</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/></exception>
    public static void SetConcurrencyETag(this HttpResponse response, ConcurrencyToken token, bool isWeak = true) =>
        SetConcurrencyETag(response, (IConcurrencyToken)token, isWeak);

    /// <summary>
    /// Sets the HTTP <c>ETag</c> response header formatted with the specified <see cref="ConcurrencyVersion"/>.
    /// </summary>
    /// <param name="response">The outgoing HTTP response.</param>
    /// <param name="version">The concurrency version.</param>
    /// <param name="isWeak">A value indicating whether to emit a weak validator (<c>W/"..."</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/></exception>
    public static void SetConcurrencyETag(this HttpResponse response, ConcurrencyVersion version, bool isWeak = true)
    {
        ArgumentNullException.ThrowIfNull(response);

        string rawValue = version.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string etag = isWeak ? $"W/\"{rawValue}\"" : $"\"{rawValue}\"";
        response.Headers[HeaderNames.ETag] = etag;
    }

    /// <summary>
    /// Sets the HTTP <c>ETag</c> response header formatted with the specified numeric version.
    /// </summary>
    /// <param name="response">The outgoing HTTP response.</param>
    /// <param name="version">The numeric version value.</param>
    /// <param name="isWeak">A value indicating whether to emit a weak validator (<c>W/"..."</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/></exception>
    public static void SetConcurrencyETag(this HttpResponse response, long version, bool isWeak = true) =>
        SetConcurrencyETag(response, new ConcurrencyVersion(version), isWeak);

    private static string NormalizeETagValue(string raw)
    {
        string trimmed = raw.Trim();
        if (trimmed.StartsWith("W/\"", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith('\"'))
        {
            return trimmed[3..^1];
        }

        if (trimmed.StartsWith('\"') && trimmed.EndsWith('\"') && trimmed.Length >= 2)
        {
            return trimmed[1..^1];
        }

        return trimmed;
    }
}
