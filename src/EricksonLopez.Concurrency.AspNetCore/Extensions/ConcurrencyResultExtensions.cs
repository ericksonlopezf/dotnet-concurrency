// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EricksonLopez.Concurrency.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for constructing minimal API <see cref="IResult"/> responses for concurrency conflicts.
/// </summary>
public static class ConcurrencyResultExtensions
{
    /// <summary>
    /// Returns an <see cref="IResult"/> representing an RFC 7807 compliant HTTP 409 Conflict with concurrency metadata and ETag headers.
    /// </summary>
    /// <param name="resultExtensions">The result extensions factory.</param>
    /// <param name="conflict">The conflict descriptor.</param>
    /// <param name="instance">The optional request URI path.</param>
    /// <returns>A new <see cref="IResult"/> configured with the conflict details.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resultExtensions"/> or <paramref name="conflict"/> is <see langword="null"/></exception>
    public static IResult ConcurrencyConflict(
        this IResultExtensions resultExtensions,
        ConcurrencyConflict conflict,
        string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);

        return new ConcurrencyConflictHttpResult(conflict, instance);
    }
}
