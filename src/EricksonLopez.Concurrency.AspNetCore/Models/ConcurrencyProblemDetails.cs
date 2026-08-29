// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using EricksonLopez.Concurrency.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EricksonLopez.Concurrency.AspNetCore.Models;

/// <summary>
/// Represents an RFC 7807 compliant <see cref="ProblemDetails"/> payload describing an optimistic concurrency conflict.
/// </summary>
public sealed class ConcurrencyProblemDetails : ProblemDetails
{
    /// <summary>
    /// Gets or sets the specific concurrency conflict category discriminator.
    /// </summary>
    public string? ConflictType { get; set; }

    /// <summary>
    /// Gets or sets the retryability classification of the conflict.
    /// </summary>
    public string? Classification { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the conflicting entity.
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// Gets or sets the type name of the conflicting entity.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the expected version value, if applicable.
    /// </summary>
    public string? ExpectedVersion { get; set; }

    /// <summary>
    /// Gets or sets the actual version value found in persistent storage, if applicable.
    /// </summary>
    public string? ActualVersion { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyProblemDetails"/> class.
    /// </summary>
    public ConcurrencyProblemDetails()
    {
        Status = StatusCodes.Status409Conflict;
        Title = "Optimistic Concurrency Conflict";
        Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8";
    }

    /// <summary>
    /// Creates a new <see cref="ConcurrencyProblemDetails"/> from a domain <see cref="ConcurrencyConflict"/>.
    /// </summary>
    /// <param name="conflict">The conflict descriptor.</param>
    /// <param name="instance">The optional request URI path that triggered the conflict.</param>
    /// <returns>A configured <see cref="ConcurrencyProblemDetails"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="conflict"/> is <see langword="null"/></exception>
    public static ConcurrencyProblemDetails From(ConcurrencyConflict conflict, string? instance = null)
    {
        ArgumentNullException.ThrowIfNull(conflict);

        var details = new ConcurrencyProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = $"Concurrency Conflict: {conflict.ConflictType}",
            Detail = conflict.Message,
            Instance = instance,
            ConflictType = conflict.ConflictType.ToString(),
            Classification = conflict.Classification.ToString(),
            EntityId = conflict.EntityId,
            EntityType = conflict.EntityType,
            ExpectedVersion = conflict.ExpectedVersion.HasValue
                ? (conflict.ExpectedVersion.Value.Kind == ExpectedVersionKind.Specific
                    ? conflict.ExpectedVersion.Value.Version.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : conflict.ExpectedVersion.Value.Kind.ToString())
                : null,
            ActualVersion = conflict.ActualVersion.HasValue
                ? (conflict.ActualVersion.Value.Exists
                    ? conflict.ActualVersion.Value.Version.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "NotFound")
                : null
        };

        foreach (KeyValuePair<string, string> item in conflict.Metadata)
        {
            details.Extensions[item.Key] = item.Value;
        }

        return details;
    }
}
