// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.Diagnostics;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class ConcurrencyControllerTests : IDisposable
{
    private sealed class ProductAggregate : IMutableVersionedEntity, IConcurrencyAware
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
            if (inst.Name == "concurrency.successes")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type" && tag.Value?.ToString() == nameof(ProductAggregate))
                    {
                        successCount += measurement;
                    }
                }
            }
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
                if (a.OperationName == "concurrency.verify_version" && a.GetTagItem("concurrency.entity_id") is "p1_ver_stale")
                {
                    stoppedActivity = a;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p1_ver_stale", Version = 11 };
        ConcurrencyConflict? conflict = _controller.VerifyVersion(product, ExpectedVersion.Specific(10), "p1_ver_stale");

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
            if (inst.Name == "concurrency.successes")
            {
                foreach (var tag in tags)
                {
                    if (tag.Key == "concurrency.entity_type" && tag.Value?.ToString() == nameof(ProductAggregate))
                    {
                        successCount += measurement;
                    }
                }
            }
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
                if (a.OperationName == "concurrency.verify_token" && a.GetTagItem("concurrency.entity_id") is "p1_tok_mismatch")
                {
                    stoppedActivity = a;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p1_tok_mismatch", Version = 5 };
        var expectedToken = new ConcurrencyToken("99", "Numeric");

        ConcurrencyConflict? conflict = _controller.VerifyToken(product, expectedToken, "p1_tok_mismatch");
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
                if (a.OperationName == "concurrency.execute_cas" && a.GetTagItem("concurrency.entity_id") is "p100_cas_match")
                {
                    stoppedActivity = a;
                }
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

        var product = new ProductAggregate { Id = "p100_cas_match", Name = "Original", Version = 1 };

        CasResult<ProductAggregate> result = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "p100_cas_match",
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
                if (a.OperationName == "concurrency.execute_cas" && a.GetTagItem("concurrency.entity_id") is "p100_cas_stale")
                {
                    stoppedActivity = a;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = "p100_cas_stale", Name = "Original", Version = 2 };

        CasResult<ProductAggregate> result = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "p100_cas_stale",
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

        var actNullId = async () => await _controller.ExecuteCasAsync(product, ExpectedVersion.Specific(1), null!, mutate);
        await actNullId.Should().ThrowAsync<ArgumentNullException>();

        var actEmptyId = async () => await _controller.ExecuteCasAsync(product, ExpectedVersion.Specific(1), "", mutate);
        await actEmptyId.Should().ThrowAsync<ArgumentException>();

        var actWhitespaceId = async () => await _controller.ExecuteCasAsync(product, ExpectedVersion.Specific(1), "   ", mutate);
        await actWhitespaceId.Should().ThrowAsync<ArgumentException>();

        var disposedController = new ConcurrencyController();
        disposedController.Dispose();
        var actDisposed = async () => await disposedController.ExecuteCasAsync(product, ExpectedVersion.Specific(1), "p1", mutate);
        var exDisposed = await actDisposed.Should().ThrowAsync<ObjectDisposedException>();
        exDisposed.Which.ObjectName.Should().Contain(nameof(ConcurrencyController));

        var recordedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.GetTagItem("concurrency.entity_id") is "p1_cancelled")
                {
                    recordedActivities.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var actCancelled = async () => await _controller.ExecuteCasAsync(product, ExpectedVersion.Specific(1), "p1_cancelled", mutate, cts.Token);
        await actCancelled.Should().ThrowAsync<OperationCanceledException>();
        recordedActivities.Should().BeEmpty();
        _controller.ActiveLockCount.Should().Be(0);
    }

    [Fact]
    public async Task ControllerOperations_WithActiveActivityListener_ShouldTraceCorrectly()
    {
        string? lastOperation = null;
        const string entityId = "p1_trace_op";
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.GetTagItem("concurrency.entity_id") is entityId)
                {
                    lastOperation = a.OperationName;
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var product = new ProductAggregate { Id = entityId, Version = 1 };

        // VerifyVersion
        _controller.VerifyVersion(product, ExpectedVersion.Specific(1), entityId);
        lastOperation.Should().Be("concurrency.verify_version");

        _controller.VerifyVersion(product, ExpectedVersion.Specific(99), entityId);
        lastOperation.Should().Be("concurrency.verify_version");

        // VerifyToken
        _controller.VerifyToken(product, new ConcurrencyToken("1", "Numeric"), entityId);
        lastOperation.Should().Be("concurrency.verify_token");

        _controller.VerifyToken(product, new ConcurrencyToken("99", "Numeric"), entityId);
        lastOperation.Should().Be("concurrency.verify_token");

        // ExecuteCasAsync
        await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            entityId,
            (p, ct) => ValueTask.FromResult(p));
        lastOperation.Should().Be("concurrency.execute_cas");

        await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(99),
            entityId,
            (p, ct) => ValueTask.FromResult(p));
        lastOperation.Should().Be("concurrency.execute_cas");
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenLockAcquisitionTimesOut_ShouldThrowTimeoutException()
    {
        var options = new ConcurrencyOptions
        {
            DefaultMaxAcquisitionTimeout = TimeSpan.FromMilliseconds(60)
        };
        using var controller = new ConcurrencyController(options: options);
        var product = new ProductAggregate { Id = "timeout_ent", Version = 1 };

        var holderEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var holderRelease = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var holdingTask = Task.Run(async () =>
        {
            try
            {
                await controller.ExecuteCasAsync(
                    product,
                    ExpectedVersion.Specific(1),
                    product.Id,
                    async (p, ct) =>
                    {
                        holderEntered.TrySetResult();
                        await holderRelease.Task;
                        return p;
                    });
            }
            catch (Exception ex)
            {
                holderEntered.TrySetException(ex);
                throw;
            }
        });

        await Task.WhenAny(holderEntered.Task, holdingTask);
        if (holdingTask.IsFaulted)
        {
            await holdingTask;
        }
        if (!holderEntered.Task.IsCompleted)
        {
            throw new InvalidOperationException("The holding task completed without acquiring the lock and entering the mutation delegate.");
        }
        await holderEntered.Task;

        Func<Task> competingAction = async () =>
        {
            await controller.ExecuteCasAsync(
                product,
                ExpectedVersion.Specific(1),
                product.Id,
                (p, ct) => ValueTask.FromResult(p));
        };

        try
        {
            await competingAction.Should().ThrowAsync<TimeoutException>()
                .WithMessage("*could not be acquired within the configured timeout*");
        }
        finally
        {
            holderRelease.TrySetResult();
        }

        await holdingTask;

        // Controller must remain healthy after lock timeout
        CasResult<ProductAggregate> recovery = await controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(product.Version),
            product.Id,
            (p, ct) => ValueTask.FromResult(p));
        recovery.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenMutationExecutionTimesOut_ShouldThrowTimeoutException()
    {
        var options = new ConcurrencyOptions
        {
            DefaultMaxExecutionTimeout = TimeSpan.FromMilliseconds(50)
        };
        using var controller = new ConcurrencyController(options: options);
        var product = new ProductAggregate { Id = "exec_timeout_ent", Version = 1 };

        Func<Task> act = async () =>
        {
            await controller.ExecuteCasAsync(
                product,
                ExpectedVersion.Specific(1),
                product.Id,
                async (p, ct) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(250), ct);
                    return p;
                });
        };

        await act.Should().ThrowAsync<TimeoutException>()
            .WithMessage("*exceeded the maximum execution timeout*");
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenReentrantInvocationDetected_ShouldThrowInvalidOperationException()
    {
        using var controller = new ConcurrencyController();
        var product = new ProductAggregate { Id = "reentrant_ent", Version = 1 };

        Func<Task> act = async () =>
        {
            await controller.ExecuteCasAsync(
                product,
                ExpectedVersion.Specific(1),
                product.Id,
                async (p, ct) =>
                {
                    // Nested reentrant call with same entityId on same async execution flow
                    await controller.ExecuteCasAsync(
                        p,
                        ExpectedVersion.Specific(1),
                        product.Id,
                        (nested, nestedCt) => ValueTask.FromResult(nested),
                        cancellationToken: ct);
                    return p;
                });
        };

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Reentrant CAS invocation detected*");
    }

    [Fact]
    public async Task ConcurrencyController_WhenDisposed_MethodsShouldThrowObjectDisposedException()
    {
        var controller = new ConcurrencyController();
        controller.Dispose();

        var product = new ProductAggregate { Id = "disposed_ent", Version = 1 };

        Action actVerifyVersion = () => controller.VerifyVersion(product, ExpectedVersion.Specific(1), product.Id);
        actVerifyVersion.Should().Throw<ObjectDisposedException>();

        Action actVerifyToken = () => controller.VerifyToken(product, new ConcurrencyToken("1"), product.Id);
        actVerifyToken.Should().Throw<ObjectDisposedException>();

        Func<Task> actCas = async () =>
        {
            await controller.ExecuteCasAsync(
                product,
                ExpectedVersion.Specific(1),
                product.Id,
                (p, ct) => ValueTask.FromResult(p));
        };
        await actCas.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public void Constructor_WithZeroOrNegativeStripeCount_ShouldDefaultToAtLeastOne()
    {
        var options = new ConcurrencyOptions { StripeCount = 0 };
        using var controller = new ConcurrencyController(options: options);
        var product = new ProductAggregate { Id = "p1", Version = 1 };
        controller.VerifyVersion(product, ExpectedVersion.Specific(1), "p1").Should().BeNull();
    }

    [Fact]
    public void VerifyVersion_And_VerifyToken_WhitespaceEntityId_ShouldThrowArgumentException()
    {
        var product = new ProductAggregate { Id = "p1", Version = 1 };

        Action actV1 = () => _controller.VerifyVersion(product, ExpectedVersion.Specific(1), "");
        actV1.Should().Throw<ArgumentException>().WithParameterName("entityId");
        Action actV2 = () => _controller.VerifyVersion(product, ExpectedVersion.Specific(1), "   ");
        actV2.Should().Throw<ArgumentException>().WithParameterName("entityId");

        Action actT1 = () => _controller.VerifyToken(product, new ConcurrencyToken("1"), "");
        actT1.Should().Throw<ArgumentException>().WithParameterName("entityId");
        Action actT2 = () => _controller.VerifyToken(product, new ConcurrencyToken("1"), "   ");
        actT2.Should().Throw<ArgumentException>().WithParameterName("entityId");
    }

    [Fact]
    public async Task VerifyVersion_And_VerifyToken_DiagnosticsAndDetailedActivityTags()
    {
        var recordedActivities = new System.Collections.Generic.List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.GetTagItem("concurrency.entity_id") is "diag-prod")
                {
                    recordedActivities.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var options = new ConcurrencyOptions
        {
            EnableDiagnostics = true,
            RecordDetailedActivityTags = true
        };
        using var controller = new ConcurrencyController(options: options);
        var product = new ProductAggregate { Id = "diag-prod", Version = 7 };

        controller.VerifyVersion(product, ExpectedVersion.Specific(7), "diag-prod");
        var versionActivity = recordedActivities.Find(a => a.OperationName == "concurrency.verify_version");
        versionActivity.Should().NotBeNull();
        versionActivity!.GetTagItem("concurrency.expected_version").Should().Be(ExpectedVersion.Specific(7).ToString());
        versionActivity.GetTagItem("concurrency.actual_version").Should().Be("7");

        var token = new ConcurrencyToken("7", "Numeric");
        controller.VerifyToken(product, token, "diag-prod");
        var tokenActivity = recordedActivities.Find(a => a.OperationName == "concurrency.verify_token");
        tokenActivity.Should().NotBeNull();
        tokenActivity!.GetTagItem("concurrency.expected_token").Should().Be("7");
        tokenActivity.GetTagItem("concurrency.actual_token").Should().Be("7");

        // Verify with EnableDiagnostics = false that NO activities are created
        recordedActivities.Clear();
        using var noDiagController = new ConcurrencyController(options: new ConcurrencyOptions { EnableDiagnostics = false });
        noDiagController.VerifyVersion(product, ExpectedVersion.Specific(7), "diag-prod");
        noDiagController.VerifyToken(product, token, "diag-prod");
        await noDiagController.ExecuteCasAsync(product, ExpectedVersion.Specific(7), "diag-prod", (p, ct) => ValueTask.FromResult(p));
        recordedActivities.Should().BeEmpty();
    }

    [Fact]
    public async Task VerifyVersion_And_VerifyToken_WhenThrowOnUnresolvedConflictIsTrue()
    {
        using var controller = new ConcurrencyController(options: new ConcurrencyOptions { ThrowOnUnresolvedConflict = true });
        var product = new ProductAggregate { Id = "throw-prod", Version = 1 };

        Action actV = () => controller.VerifyVersion(product, ExpectedVersion.Specific(2), "throw-prod");
        actV.Should().Throw<ConcurrencyException>();

        Action actT = () => controller.VerifyToken(product, new ConcurrencyToken("2"), "throw-prod");
        actT.Should().Throw<ConcurrencyException>();

        Func<Task> actCas = async () => await controller.ExecuteCasAsync(product, ExpectedVersion.Specific(2), "throw-prod", (p, ct) => ValueTask.FromResult(p));
        await actCas.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task ExecuteCasAsync_ConcurrentInvocationsForSameEntity_ShouldCorrectlyIncrementAndDecrementRefCount()
    {
        var product1 = new ProductAggregate { Id = "shared_entity", Version = 1 };
        var tcsStart = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var tcsRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task1 = Task.Run(async () =>
        {
            try
            {
                return await _controller.ExecuteCasAsync(
                    product1,
                    ExpectedVersion.Specific(1),
                    "shared_entity",
                    async (p, ct) =>
                    {
                        tcsStart.TrySetResult(true);
                        await tcsRelease.Task;
                        return p;
                    });
            }
            catch (Exception ex)
            {
                tcsStart.TrySetException(ex);
                throw;
            }
        });

        await Task.WhenAny(tcsStart.Task, task1);
        if (task1.IsFaulted)
        {
            await task1;
        }
        if (!tcsStart.Task.IsCompleted)
        {
            throw new InvalidOperationException("Task1 completed without entering mutation delegate.");
        }
        await tcsStart.Task;

        _controller.GetLockRefCount("shared_entity").Should().Be(1);
        _controller.ActiveLockCount.Should().Be(1);

        var tcsTask2Release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var product2 = new ProductAggregate { Id = "shared_entity", Version = 2 };
        var task2 = Task.Run(async () =>
        {
            return await _controller.ExecuteCasAsync(
                product2,
                ExpectedVersion.Specific(2),
                "shared_entity",
                async (p, ct) =>
                {
                    await tcsTask2Release.Task;
                    return p;
                });
        });

        for (int i = 0; i < 50; i++)
        {
            if (_controller.GetLockRefCount("shared_entity") == 2)
            {
                break;
            }
            await Task.Delay(10);
        }

        _controller.GetLockRefCount("shared_entity").Should().Be(2);

        tcsRelease.TrySetResult(true);

        var res1 = await task1;
        res1.IsSuccess.Should().BeTrue();

        // While task2 is holding the lock, it must NOT be pooled yet and must remain in dictionary
        _controller.PooledLockCount.Should().Be(0);
        _controller.ActiveLockCount.Should().Be(1);
        _controller.GetLockRefCount("shared_entity").Should().Be(1);

        tcsTask2Release.TrySetResult(true);

        var res2 = await task2;
        res2.IsSuccess.Should().BeTrue();

        _controller.ActiveLockCount.Should().Be(0);
        _controller.GetLockRefCount("shared_entity").Should().BeNull();
        _controller.PooledLockCount.Should().BeGreaterThanOrEqualTo(1);

        _controller.LockPool.TryPeek(out var pooledLock).Should().BeTrue();
        pooledLock!.RefCount.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenDisposedDuringExecution_ShouldDisposeActiveLockInReleaseLock()
    {
        var controller = new ConcurrencyController();
        var product = new ProductAggregate { Id = "dispose_while_active", Version = 1 };
        var tcsStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var casTask = Task.Run(async () =>
        {
            try
            {
                return await controller.ExecuteCasAsync(
                    product,
                    ExpectedVersion.Specific(1),
                    "dispose_while_active",
                    async (p, ct) =>
                    {
                        tcsStarted.TrySetResult(true);
                        await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
                        return p;
                    });
            }
            catch (Exception ex)
            {
                tcsStarted.TrySetException(ex);
                throw;
            }
        });

        await Task.WhenAny(tcsStarted.Task, casTask);
        if (casTask.IsFaulted)
        {
            await casTask;
        }
        if (!tcsStarted.Task.IsCompleted)
        {
            throw new InvalidOperationException("CasTask completed without entering mutation delegate.");
        }
        await tcsStarted.Task;

        var activeLock = controller.GetActiveLock("dispose_while_active");
        activeLock.Should().NotBeNull();
        controller.ActiveLockCount.Should().Be(1);

        controller.Dispose();

        controller.DisposeCts.IsCancellationRequested.Should().BeTrue();
        controller.ActiveLockCount.Should().Be(0);

        var act = () => casTask;
        await act.Should().ThrowAsync<OperationCanceledException>();

        controller.PooledLockCount.Should().Be(0);
        Action actSemaphore = () => activeLock!.Semaphore.Wait(0);
        actSemaphore.Should().Throw<ObjectDisposedException>();
    }

    private sealed class ImmutableProduct : IVersionedEntity
    {
        public long Version { get; init; } = 1;
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenImmutableEntityDoesNotAdvanceVersion_ShouldThrowInvalidOperationException()
    {
        var entity = new ImmutableProduct { Version = 1 };
        Func<Task> act = async () =>
        {
            await _controller.ExecuteCasAsync(
                entity,
                ExpectedVersion.Specific(1),
                "imm-1",
                (e, ct) => ValueTask.FromResult(e));
        };

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.WithMessage("*In-memory CAS requires version progression to prevent stale updates.*");
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenMutableEntity_ShouldMutateVersionOnEntityInstance()
    {
        var entity = new ProductAggregate { Id = "mut-1", Version = 5 };
        var result = await _controller.ExecuteCasAsync(
            entity,
            ExpectedVersion.Specific(5),
            "mut-1",
            (e, ct) => ValueTask.FromResult(e));

        result.IsSuccess.Should().BeTrue();
        entity.Version.Should().Be(6);
    }

    [Fact]
    public async Task ConcurrencyController_DisposeIdempotent_And_DisposeAsync_ShouldWork()
    {
        var controller = new ConcurrencyController();
        var product = new ProductAggregate { Id = "p-pool", Version = 1 };
        var res = await controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "p-pool",
            (p, ct) => ValueTask.FromResult(p));
        res.IsSuccess.Should().BeTrue();

        controller.Dispose();
        controller.Dispose();

        var controller2 = new ConcurrencyController();
        await controller2.DisposeAsync();
        Action act = () => controller2.VerifyVersion(product, ExpectedVersion.Specific(1), "p-pool");
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public async Task ConcurrencyController_WhenStripeCountIsZeroOrNegative_ShouldClampToOne()
    {
        var options = new ConcurrencyOptions { StripeCount = 0 };
        using var controller = new ConcurrencyController(options);
        var product = new ProductAggregate { Id = "stripe_zero", Version = 1 };

        var result = await controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "stripe_zero",
            (p, ct) => ValueTask.FromResult(p));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCasAsync_WhenToctouConflictOccurs_ShouldRecordConflictAndHandleAccordingToOptions()
    {
        var product = new ProductAggregate { Id = "toctou_1", Version = 1 };
        var recordedActivities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == ConcurrencyDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = a =>
            {
                if (a.GetTagItem("concurrency.entity_id") is "toctou_1")
                {
                    recordedActivities.Add(a);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);

        var result = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "toctou_1",
            (p, ct) =>
            {
                p.Version = 999;
                return ValueTask.FromResult(p);
            });

        result.IsSuccess.Should().BeFalse();
        result.Conflict.Should().NotBeNull();
        result.Conflict!.ActualVersion!.Value.Version.Value.Should().Be(999);
        var act = recordedActivities.Find(a => a.OperationName == "concurrency.execute_cas");
        act.Should().NotBeNull();
        act!.GetTagItem("concurrency.conflict").Should().Be(true);

        using var throwingController = new ConcurrencyController(options: new ConcurrencyOptions { ThrowOnUnresolvedConflict = true });
        var product2 = new ProductAggregate { Id = "toctou_2", Version = 1 };
        Func<Task> actThrow = async () => await throwingController.ExecuteCasAsync(
            product2,
            ExpectedVersion.Specific(1),
            "toctou_2",
            (p, ct) =>
            {
                p.Version = 999;
                return ValueTask.FromResult(p);
            });

        var ex = await actThrow.Should().ThrowAsync<ConcurrencyException>();
        ex.Which.Conflict.Should().NotBeNull();
        ex.Which.Conflict!.ActualVersion!.Value.Version.Value.Should().Be(999);
    }

    [Fact]
    public async Task ExecuteCasAsync_SequentialCallsForSameEntity_ShouldRestoreReentrancyNode()
    {
        var product = new ProductAggregate { Id = "seq_ent", Version = 1 };
        var res1 = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "seq_ent",
            (p, ct) => ValueTask.FromResult(p));
        res1.IsSuccess.Should().BeTrue();

        var res2 = await _controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(2),
            "seq_ent",
            (p, ct) => ValueTask.FromResult(p));
        res2.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteCasAsync_NestedCallsForDifferentEntities_ShouldRestoreReentrancyNodeOnCompletion()
    {
        var product1 = new ProductAggregate { Id = "parent_ent", Version = 1 };
        var product2 = new ProductAggregate { Id = "child_ent", Version = 1 };

        var res = await _controller.ExecuteCasAsync(
            product1,
            ExpectedVersion.Specific(1),
            "parent_ent",
            async (p, ct) =>
            {
                // First call to child_ent from inside parent_ent mutate
                var childRes1 = await _controller.ExecuteCasAsync(
                    product2,
                    ExpectedVersion.Specific(1),
                    "child_ent",
                    (c, ct2) => ValueTask.FromResult(c),
                    ct);
                childRes1.IsSuccess.Should().BeTrue();

                // Sequential second call to child_ent inside parent_ent mutate must SUCCEED
                // because child_ent was popped from _activeEntityLocks in finally block!
                var childRes2 = await _controller.ExecuteCasAsync(
                    product2,
                    ExpectedVersion.Specific(2),
                    "child_ent",
                    (c, ct2) => ValueTask.FromResult(c),
                    ct);
                childRes2.IsSuccess.Should().BeTrue();

                return p;
            });

        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Dispose_ShouldClearDictionaries_DisposePooledLocks_AndDisposeCts()
    {
        var controller = new ConcurrencyController();
        var product = new ProductAggregate { Id = "disp_test", Version = 1 };

        var res = await controller.ExecuteCasAsync(
            product,
            ExpectedVersion.Specific(1),
            "disp_test",
            (p, ct) => ValueTask.FromResult(p));
        res.IsSuccess.Should().BeTrue();

        controller.PooledLockCount.Should().BeGreaterThanOrEqualTo(1);
        controller.LockPool.TryPeek(out var pooledLock).Should().BeTrue();
        pooledLock.Should().NotBeNull();

        controller.Dispose();

        controller.ActiveLockCount.Should().Be(0);
        controller.PooledLockCount.Should().Be(0);
        Action actLock = () => pooledLock!.Semaphore.Wait(0);
        actLock.Should().Throw<ObjectDisposedException>();

        Action actCts = () => { var _ = controller.DisposeCts.Token; };
        actCts.Should().Throw<ObjectDisposedException>();
    }

    public void Dispose()
    {
        _controller.Dispose();
    }
}

