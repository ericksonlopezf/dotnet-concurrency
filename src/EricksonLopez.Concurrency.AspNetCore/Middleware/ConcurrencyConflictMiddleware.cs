// Copyright © Erickson Lopez. MIT License.
using System;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using EricksonLopez.Concurrency.AspNetCore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EricksonLopez.Concurrency.AspNetCore.Middleware;

/// <summary>
/// Intercepts unhandled <see cref="ConcurrencyException"/> instances in the HTTP pipeline and writes RFC 7807 compliant HTTP 409 Conflict responses.
/// </summary>
public sealed partial class ConcurrencyConflictMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConcurrencyConflictMiddleware>? _logger;

    private static readonly Action<ILogger, string, Exception?> LogConflictDetectedCallback =
        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1, nameof(ConcurrencyConflictMiddleware)), "Optimistic concurrency conflict intercepted: {Message}");

    private static readonly Action<ILogger, Exception?> LogResponseAlreadyStartedCallback =
        LoggerMessage.Define(LogLevel.Warning, new EventId(2, nameof(ConcurrencyConflictMiddleware)), "ConcurrencyException was thrown but HTTP response has already started.");

    /// <summary>
    /// Initializes a new instance of the <see cref="ConcurrencyConflictMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the execution pipeline.</param>
    /// <param name="logger">An optional logger instance.</param>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/></exception>
    public ConcurrencyConflictMiddleware(RequestDelegate next, ILogger<ConcurrencyConflictMiddleware>? logger = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger;
    }

    /// <summary>
    /// Executes the middleware pipeline logic.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <returns>A task representing the asynchronous middleware execution.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/></exception>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (ConcurrencyException ex)
        {
            if (context.Response.HasStarted)
            {
                if (_logger is not null)
                {
                    LogResponseAlreadyStartedCallback(_logger, ex);
                }
                throw;
            }

            await HandleConcurrencyExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private async Task HandleConcurrencyExceptionAsync(HttpContext context, ConcurrencyException exception)
    {
        if (_logger is not null)
        {
            LogConflictDetectedCallback(_logger, exception.Message, exception);
        }

        context.Response.Clear();

        ConcurrencyConflict conflict = exception.Conflict ?? new ConcurrencyConflict(
            entityId: "unknown",
            entityType: "unknown",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: context.Request.Method,
            message: exception.Message);

        if (conflict.ActualToken is not null && !conflict.ActualToken.IsEmpty)
        {
            context.Response.SetConcurrencyETag(conflict.ActualToken);
        }
        else if (conflict.ActualVersion is { } actual && actual.Exists)
        {
            context.Response.SetConcurrencyETag(actual.Version);
        }

        ConcurrencyProblemDetails problemDetails = ConcurrencyProblemDetails.From(conflict, context.Request.Path);
        await Results.Problem(problemDetails).ExecuteAsync(context).ConfigureAwait(false);
    }
}
