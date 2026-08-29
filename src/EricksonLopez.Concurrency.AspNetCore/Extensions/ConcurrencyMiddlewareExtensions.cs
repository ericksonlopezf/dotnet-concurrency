// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Middleware;
using Microsoft.AspNetCore.Builder;

namespace EricksonLopez.Concurrency.AspNetCore.Extensions;

/// <summary>
/// Provides extension methods for registering concurrency middleware into an <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ConcurrencyMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="ConcurrencyConflictMiddleware"/> to the application pipeline to automatically convert <see cref="ConcurrencyException"/> into RFC 7807 HTTP 409 Conflict responses.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder instance for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="app"/> is <see langword="null"/></exception>
    public static IApplicationBuilder UseConcurrencyConflictHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<ConcurrencyConflictMiddleware>();
    }
}
