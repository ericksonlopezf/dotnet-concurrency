// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Diagnostics;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Mediator;
using Xunit;

namespace EricksonLopez.Concurrency.Mediator.Tests;

public sealed class ConcurrencyBehaviorTests
{
    private sealed record PlainCommand(string Data) : ICommand<string>;

    private sealed record ConcurrencyAwareCommand(
        string CustomerId,
        ExpectedVersion? ExpectedVersion = null,
        IConcurrencyToken? ConcurrencyToken = null)
        : IConcurrencyAwareRequest<string>
    {
        ExpectedVersion? IConcurrencyAwareRequest.ExpectedVersion => ExpectedVersion;
        IConcurrencyToken? IConcurrencyAwareRequest.ConcurrencyToken => ConcurrencyToken;
    }

    private sealed record StubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    private readonly struct MockNext : INext<string>
    {
        private readonly string _result;
        public MockNext(string result) => _result = result;
        public ValueTask<string> InvokeAsync() => ValueTask.FromResult(_result);
    }

    private readonly struct FailingNext : INext<string>
    {
        private readonly Exception _exception;
        public FailingNext(Exception exception) => _exception = exception;
        public ValueTask<string> InvokeAsync() => throw _exception;
    }

    [Fact]
    public async Task Handle_NonConcurrencyAwareRequest_ShouldPassThroughWithoutTracking()
    {
        var behavior = new ConcurrencyBehavior<PlainCommand, string>();
        var command = new PlainCommand("Test");
        var next = new MockNext("Processed");

        string result = await behavior.Handle(command, next, CancellationToken.None);

        result.Should().Be("Processed");
    }

    [Fact]
    public async Task Handle_ConcurrencyAwareRequest_Success_WithActivityAndTags()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var durationMeasurements = new List<double>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, meterListenerInstance) =>
        {
            if (instrument.Meter.Name == ConcurrencyDiagnostics.SourceName && instrument.Name == "concurrency.duration")
            {
                meterListenerInstance.EnableMeasurementEvents(instrument);
            }
        };
        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            durationMeasurements.Add(measurement);
        });
        meterListener.Start();

        var behavior = new ConcurrencyBehavior<ConcurrencyAwareCommand, string>();
        var token = new StubToken("token-abc");
        var command = new ConcurrencyAwareCommand("cust_1", ExpectedVersion.Specific(10), token);
        var next = new MockNext("SuccessResult");

        string result = await behavior.Handle(command, next, CancellationToken.None);

        result.Should().Be("SuccessResult");

        activities.Should().ContainSingle();
        Activity activity = activities[0];
        activity.OperationName.Should().Be("concurrency.mediator.handle");
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem("concurrency.request").Should().Be(nameof(ConcurrencyAwareCommand));
        activity.GetTagItem("concurrency.expected_version").Should().Be("[Expected:10]");
        activity.GetTagItem("concurrency.expected_token").Should().Be("token-abc");

        durationMeasurements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ConcurrencyAwareRequest_WithoutVersionOrToken_Success()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var behavior = new ConcurrencyBehavior<ConcurrencyAwareCommand, string>();
        var command = new ConcurrencyAwareCommand("cust_1", null, null);
        var next = new MockNext("SuccessWithoutConstraints");

        string result = await behavior.Handle(command, next, CancellationToken.None);

        result.Should().Be("SuccessWithoutConstraints");

        activities.Should().ContainSingle();
        Activity activity = activities[0];
        activity.Status.Should().Be(ActivityStatusCode.Ok);
        activity.GetTagItem("concurrency.expected_version").Should().BeNull();
        activity.GetTagItem("concurrency.expected_token").Should().BeNull();
    }

    [Fact]
    public async Task Handle_ConcurrencyAwareRequest_WhenConcurrencyException_ShouldRecordConflictAndRethrow()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var conflictMeasurements = new List<(long value, string? requestName)>();
        var durationMeasurements = new List<double>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, meterListenerInstance) =>
        {
            if (instrument.Meter.Name == ConcurrencyDiagnostics.SourceName)
            {
                if (instrument.Name == "concurrency.conflicts" || instrument.Name == "concurrency.duration")
                {
                    meterListenerInstance.EnableMeasurementEvents(instrument);
                }
            }
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            string? reqTag = null;
            foreach (var tag in tags)
            {
                if (tag.Key == "concurrency.request") reqTag = tag.Value?.ToString();
            }
            conflictMeasurements.Add((measurement, reqTag));
        });
        meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            durationMeasurements.Add(measurement);
        });
        meterListener.Start();

        var behavior = new ConcurrencyBehavior<ConcurrencyAwareCommand, string>();
        var command = new ConcurrencyAwareCommand("cust_1", ExpectedVersion.Specific(5));
        var next = new FailingNext(new ConcurrencyException("Optimistic version mismatch detected"));

        var act = async () => await behavior.Handle(command, next, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("Optimistic version mismatch detected");

        activities.Should().ContainSingle();
        Activity activity = activities[0];
        activity.Status.Should().Be(ActivityStatusCode.Error);
        activity.StatusDescription.Should().Be("Optimistic version mismatch detected");
        activity.GetTagItem("concurrency.conflict").Should().Be(true);

        conflictMeasurements.Should().ContainSingle();
        conflictMeasurements[0].value.Should().Be(1);
        conflictMeasurements[0].requestName.Should().Be(nameof(ConcurrencyAwareCommand));

        durationMeasurements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ConcurrencyAwareRequest_WithoutActivityListener_Success()
    {
        var behavior = new ConcurrencyBehavior<ConcurrencyAwareCommand, string>();
        var command = new ConcurrencyAwareCommand("cust_1", ExpectedVersion.Specific(1));
        var next = new MockNext("NoActivitySuccess");

        string result = await behavior.Handle(command, next, CancellationToken.None);

        result.Should().Be("NoActivitySuccess");
    }

    [Fact]
    public async Task Handle_ConcurrencyAwareRequest_WithoutActivityListener_WhenConcurrencyException()
    {
        var behavior = new ConcurrencyBehavior<ConcurrencyAwareCommand, string>();
        var command = new ConcurrencyAwareCommand("cust_1", ExpectedVersion.Specific(1));
        var next = new FailingNext(new ConcurrencyException("Conflict with no activity listener"));

        var act = async () => await behavior.Handle(command, next, CancellationToken.None);

        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("Conflict with no activity listener");
    }

    [Fact]
    public async Task Handle_ConcurrencyAwareRequest_WhenGenericException_ShouldNotCatchAsConcurrencyException()
    {
        var behavior = new ConcurrencyBehavior<ConcurrencyAwareCommand, string>();
        var command = new ConcurrencyAwareCommand("cust_1");
        var next = new FailingNext(new InvalidOperationException("Generic failure"));

        var act = async () => await behavior.Handle(command, next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Generic failure");
    }
}
