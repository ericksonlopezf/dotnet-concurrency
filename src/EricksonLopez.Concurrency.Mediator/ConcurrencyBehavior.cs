// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;
using EricksonLopez.Mediator;

namespace EricksonLopez.Concurrency.Mediator;

/// <summary>
/// Represents an observability pipeline behavior intercepting <see cref="IConcurrencyAwareRequest"/> commands to track concurrency telemetry, distributed trace spans, and metric counters.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Important Architectural Notice:</strong> This pipeline behavior is strictly focused on <strong>observability and telemetry</strong>
/// (OpenTelemetry <c>concurrency.mediator.handle</c> activity tags and metric tracking). It does <em>not</em> automatically verify or enforce entity version preconditions against persistent storage.
/// </para>
/// <para>
/// Optimistic version enforcement and CAS state transitions must be executed explicitly inside the request handler using an injected <see cref="IConcurrencyController"/>.
/// </para>
/// </remarks>
/// <typeparam name="TRequest">The request type being processed.</typeparam>
/// <typeparam name="TResponse">The response type returned by the downstream pipeline.</typeparam>
public sealed class ConcurrencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    /// <inheritdoc />
    public async ValueTask<TResponse> Handle<TNext>(
        TRequest request,
        TNext next,
        CancellationToken cancellationToken)
        where TNext : struct, INext<TResponse>
    {
        if (request is not IConcurrencyAwareRequest concurrencyRequest)
        {
            return await next.InvokeAsync().ConfigureAwait(false);
        }

        string requestName = typeof(TRequest).Name;
        using Activity? activity = ConcurrencyDiagnostics.ActivitySource.StartActivity("concurrency.mediator.handle");

        if (activity is not null)
        {
            activity.SetTag("concurrency.request", requestName);
            if (concurrencyRequest.ExpectedVersion.HasValue)
            {
                activity.SetTag("concurrency.expected_version", concurrencyRequest.ExpectedVersion.Value.ToString());
            }

            if (concurrencyRequest.ConcurrencyToken is not null)
            {
                activity.SetTag("concurrency.expected_token", concurrencyRequest.ConcurrencyToken.Value);
            }
        }

        long start = Stopwatch.GetTimestamp();
        TResponse response = default!;
        try
        {
            response = await next.InvokeAsync().ConfigureAwait(false);
        }
        catch (ConcurrencyException ex)
        {
            RecordConflict(ex, start, activity, requestName);
            throw;
        }

        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        ConcurrencyDiagnostics.OperationDurationHistogram.Record(elapsedMs);
        activity?.SetStatus(ActivityStatusCode.Ok);

        return response;
    }

    private static void RecordConflict(
        ConcurrencyException ex,
        long startTimestamp,
        Activity? activity,
        string requestName)
    {
        double elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        ConcurrencyDiagnostics.OperationDurationHistogram.Record(elapsedMs);

        if (activity is not null)
        {
            activity.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity.SetTag("concurrency.conflict", true);
        }

        ConcurrencyDiagnostics.ConflictsCounter.Add(1, new KeyValuePair<string, object?>("concurrency.request", requestName));
    }
}
