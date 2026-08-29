// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Diagnostics;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class ConcurrencyControllerTests
{
    private sealed class ProductAggregate : IVersionedEntity, IConcurrencyAware
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Version { get; set; }
        public IConcurrencyToken ConcurrencyToken => new ConcurrencyToken(Version.ToString(System.Globalization.CultureInfo.InvariantCulture), "Numeric");
    }

    private sealed class CustomStubChecker : IConcurrencyChecker
    {
        public bool CheckVersion(
            ExpectedVersion expected,
            ConcurrencyVersion actual,
            string entityId,
            string entityType,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out ConcurrencyConflict? conflict)
        {
            conflict = new ConcurrencyConflict(entityId, entityType, ConcurrencyConflictType.Custom, ConcurrencyConflictClassification.Transient, "TestOp", "Stub version conflict");
            return false;
        }

        public bool CheckToken(
            IConcurrencyToken expected,
            IConcurrencyToken actual,
            string entityId,
            string entityType,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out ConcurrencyConflict? conflict)
        {
            conflict = new ConcurrencyConflict(entityId, entityType, ConcurrencyConflictType.Custom, ConcurrencyConflictClassification.Transient, "TestOp", "Stub token conflict");
            return false;
        }
    }

    private readonly ConcurrencyController _controller = new();

    [Fact]
    public void Constructor_WithCustomChecker_ShouldUseSpecifiedChecker()
    {
        var customChecker = new CustomStubChecker();
        var controller = new ConcurrencyController(customChecker);

        var product = new ProductAggregate { Id = "p1", Version = 1 };
        var conflict = controller.VerifyVersion(product, ExpectedVersion.Specific(1), "p1");
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.Custom);
        conflict.Message.Should().Be("Stub version conflict");

        var tokenConflict = controller.VerifyToken(product, new ConcurrencyToken("1", "Numeric"), "p1");
        tokenConflict.Should().NotBeNull();
        tokenConflict!.ConflictType.Should().Be(ConcurrencyConflictType.Custom);
        tokenConflict.Message.Should().Be("Stub token conflict");
    }

    [Fact]
    public void VerifyVersion_WhenValid_ShouldReturnNullAndRecordSuccess()
    {
        long successCount = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.successes") successCount += measurement;
        });
        meterListener.Start();

        var product = new ProductAggregate { Id = "p1", Version = 10 };
        ConcurrencyConflict? conflict = _controller.VerifyVersion(product, ExpectedVersion.Specific(10), "p1");

        conflict.Should().BeNull();
        successCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void VerifyVersion_WhenStale_ShouldReturnConflictAndTagActivity()
    {
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.OperationName == "concurrency.verify_version") stoppedActivity = a;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p1", Version = 11 };
        ConcurrencyConflict? conflict = _controller.VerifyVersion(product, ExpectedVersion.Specific(10), "p1");

        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);

        stoppedActivity.Should().NotBeNull();
        stoppedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        stoppedActivity.StatusDescription.Should().Be("Concurrency conflict: VersionMismatch");
        stoppedActivity.GetTagItem("concurrency.conflict").Should().Be(true);
        stoppedActivity.GetTagItem("concurrency.conflict_type").Should().Be("VersionMismatch");
    }

    [Fact]
    public void VerifyVersion_NullEntity_ShouldThrowArgumentNullException()
    {
        ProductAggregate nullProduct = null!;
        Action act = () => _controller.VerifyVersion(nullProduct, ExpectedVersion.Specific(1), "p1");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }

    [Fact]
    public void VerifyToken_WhenValid_ShouldReturnNullAndRecordSuccess()
    {
        long successCount = 0;
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<long>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.successes") successCount += measurement;
        });
        meterListener.Start();

        var product = new ProductAggregate { Id = "p1", Version = 5 };
        var expectedToken = new ConcurrencyToken("5", "Numeric");

        ConcurrencyConflict? conflict = _controller.VerifyToken(product, expectedToken, "p1");
        conflict.Should().BeNull();
        successCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public void VerifyToken_WhenMismatch_ShouldReturnConflictAndTagActivity()
    {
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.OperationName == "concurrency.verify_token") stoppedActivity = a;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p1", Version = 5 };
        var expectedToken = new ConcurrencyToken("99", "Numeric");

        ConcurrencyConflict? conflict = _controller.VerifyToken(product, expectedToken, "p1");
        conflict.Should().NotBeNull();
        conflict!.ConflictType.Should().Be(ConcurrencyConflictType.TokenMismatch);

        stoppedActivity.Should().NotBeNull();
        stoppedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        stoppedActivity.StatusDescription.Should().Be("Concurrency conflict: TokenMismatch");
        stoppedActivity.GetTagItem("concurrency.conflict").Should().Be(true);
        stoppedActivity.GetTagItem("concurrency.conflict_type").Should().Be("TokenMismatch");
    }

    [Fact]
    public void VerifyToken_NullArguments_ShouldThrowArgumentNullException()
    {
        var product = new ProductAggregate { Id = "p1", Version = 5 };

        Action actNullEntity = () => _controller.VerifyToken<ProductAggregate>(null!, new ConcurrencyToken("1"), "p1");
        actNullEntity.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");

        Action actNullExpected = () => _controller.VerifyToken(product, null!, "p1");
        actNullExpected.Should().Throw<ArgumentNullException>()
            .WithParameterName("expected");
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenMatchingVersion_ShouldMutateAndIncrementVersionAndRecordMetrics()
    {
        double recordedDuration = -1;
        Activity? stoppedActivity = null;

        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.OperationName == "concurrency.execute_cas") stoppedActivity = a;
            }
        };
        ActivitySource.AddActivityListener(listener);

        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (inst, l) => { if (inst.Meter.Name == ConcurrencyDiagnostics.SourceName) l.EnableMeasurementEvents(inst); };
        meterListener.SetMeasurementEventCallback<double>((inst, measurement, tags, state) =>
        {
            if (inst.Name == "concurrency.duration") recordedDuration = measurement;
        });
        meterListener.Start();

        var product = new ProductAggregate { Id = "p100", Name = "Original", Version = 1 };

        CasResult<ProductAggregate> result = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "p100",
            (p, ct) =>
            {
                p.Name = "Updated";
                return ValueTask.FromResult(p);
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.IsConflict.Should().BeFalse();
        result.Entity.Should().NotBeNull();
        result.Entity!.Name.Should().Be("Updated");
        result.NewVersion.Should().Be(new ConcurrencyVersion(2));
        result.Conflict.Should().BeNull();

        recordedDuration.Should().BeGreaterThanOrEqualTo(0);

        stoppedActivity.Should().NotBeNull();
        stoppedActivity!.Status.Should().Be(ActivityStatusCode.Ok);
        stoppedActivity.GetTagItem("concurrency.conflict").Should().Be(false);
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenStaleVersion_ShouldReturnConflictedCasResultAndTagActivity()
    {
        Activity? stoppedActivity = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.OperationName == "concurrency.execute_cas") stoppedActivity = a;
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p100", Name = "Original", Version = 2 };

        CasResult<ProductAggregate> result = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "p100",
            (p, ct) =>
            {
                p.Name = "NeverExecuted";
                return ValueTask.FromResult(p);
            },
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.IsConflict.Should().BeTrue();
        result.Entity.Should().BeNull();
        result.Conflict.Should().NotBeNull();
        result.Conflict!.ConflictType.Should().Be(ConcurrencyConflictType.VersionMismatch);

        stoppedActivity.Should().NotBeNull();
        stoppedActivity!.Status.Should().Be(ActivityStatusCode.Error);
        stoppedActivity.StatusDescription.Should().Be("Concurrency conflict: VersionMismatch");
        stoppedActivity.GetTagItem("concurrency.conflict").Should().Be(true);
        stoppedActivity.GetTagItem("concurrency.conflict_type").Should().Be("VersionMismatch");
    }

    [Fact]
    public async Task ExecuteCasAsync_NullArgumentsAndCancellation_ShouldThrow()
    {
        var product = new ProductAggregate { Id = "p1", Version = 1 };
        Func<ProductAggregate, CancellationToken, ValueTask<ProductAggregate>> mutate =
            (p, ct) => ValueTask.FromResult(p);

        var actNullEntity = async () => await _controller.ExecuteCasAsync<ProductAggregate>(null!, ExpectedVersion.Specific(1), "p1", mutate);
        await actNullEntity.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("entity");

        var actNullMutate = async () => await _controller.ExecuteCasAsync(product, ExpectedVersion.Specific(1), "p1", null!);
        await actNullMutate.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("mutate");

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var actCancelled = async () => await _controller.ExecuteCasAsync(product, ExpectedVersion.Specific(1), "p1", mutate, cts.Token);
        await actCancelled.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ControllerOperations_WithActiveActivityListener_ShouldTraceCorrectly()
    {
        string? lastOperation = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = a => lastOperation = a.OperationName
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p1", Version = 1 };

        // VerifyVersion
        _controller.VerifyVersion(product, ExpectedVersion.Specific(1), "p1");
        lastOperation.Should().Be("concurrency.verify_version");

        _controller.VerifyVersion(product, ExpectedVersion.Specific(99), "p1");
        lastOperation.Should().Be("concurrency.verify_version");

        // VerifyToken
        _controller.VerifyToken(product, new ConcurrencyToken("1", "Numeric"), "p1");
        lastOperation.Should().Be("concurrency.verify_token");

        _controller.VerifyToken(product, new ConcurrencyToken("99", "Numeric"), "p1");
        lastOperation.Should().Be("concurrency.verify_token");

        // ExecuteCasAsync
        await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "p1",
            (p, ct) => ValueTask.FromResult(p));
        lastOperation.Should().Be("concurrency.execute_cas");

        await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(99),
            "p1",
            (p, ct) => ValueTask.FromResult(p));
        lastOperation.Should().Be("concurrency.execute_cas");
    }
}
