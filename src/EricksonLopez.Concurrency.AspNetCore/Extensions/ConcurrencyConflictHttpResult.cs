// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Models;
using Microsoft.AspNetCore.Http;

namespace EricksonLopez.Concurrency.AspNetCore.Extensions;

/// <summary>
/// Represents an <see cref="IResult"/> returned when an optimistic concurrency conflict occurs.
/// </summary>
public sealed class ConcurrencyConflictHttpResult : IResult
{
    /// <summary>
    /// Gets the conflict descriptor.
    /// </summary>
    public ConcurrencyConflict Conflict { get; }

    /// <summary>
    /// Gets the optional request URI path.
    /// </summary>
    public string? Instance { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictHttpResult"/> class.
    /// </summary>
    /// <param name="conflict">The concurrency conflict descriptor.</param>
    /// <param name="instance">The optional request URI path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="conflict"/> is <see langword="null"/></exception>
    public ConcurrencyConflictHttpResult(ConcurrencyConflict conflict, string? instance = null)
    {
        Conflict = conflict ?? throw new ArgumentNullException(nameof(conflict));
        Instance = instance;
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (Conflict.ActualToken is not null && !Conflict.ActualToken.IsEmpty)
        {
            httpContext.Response.SetConcurrencyETag(Conflict.ActualToken);
        }
        else if (Conflict.ActualVersion is { } actual && actual.Exists)
        {
            httpContext.Response.SetConcurrencyETag(actual.Version);
        }

        ConcurrencyProblemDetails problemDetails = ConcurrencyProblemDetails.From(Conflict, Instance ?? httpContext.Request.Path);
        await Results.Problem(problemDetails).ExecuteAsync(httpContext).ConfigureAwait(false);
    }
}
