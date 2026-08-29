// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.DependencyInjection;
using EricksonLopez.Concurrency.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class ConcurrencyOptionsAndDITests
{
    private sealed class SampleEntity
    {
    }

    private sealed class SampleResolver : IConcurrencyConflictResolver<SampleEntity>
    {
        public System.Threading.Tasks.ValueTask<ConflictResolution<SampleEntity>> ResolveAsync(
            SampleEntity proposedEntity,
            SampleEntity? currentDatabaseEntity,
            ConcurrencyConflict conflict,
            System.Threading.CancellationToken cancellationToken = default)
        {
            return System.Threading.Tasks.ValueTask.FromResult(ConflictResolution.Rejected<SampleEntity>());
        }
    }

    [Fact]
    public void ConcurrencyOptions_DefaultValues_ShouldBeSet()
    {
        var options = new ConcurrencyOptions();

        options.DefaultResolutionStrategy.Should().Be(ConflictResolutionStrategy.Reject);
        options.EnableDiagnostics.Should().BeTrue();
        options.DefaultConflictClassification.Should().Be(ConcurrencyConflictClassification.Transient);
        options.RecordDetailedActivityTags.Should().BeTrue();
        options.ThrowOnUnresolvedConflict.Should().BeFalse();

        // Mutate properties
        options.DefaultResolutionStrategy = ConflictResolutionStrategy.LastWriteWinsExplicit;
        options.EnableDiagnostics = false;
        options.DefaultConflictClassification = ConcurrencyConflictClassification.Fatal;
        options.RecordDetailedActivityTags = false;
        options.ThrowOnUnresolvedConflict = true;

        options.DefaultResolutionStrategy.Should().Be(ConflictResolutionStrategy.LastWriteWinsExplicit);
        options.EnableDiagnostics.Should().BeFalse();
        options.DefaultConflictClassification.Should().Be(ConcurrencyConflictClassification.Fatal);
        options.RecordDetailedActivityTags.Should().BeFalse();
        options.ThrowOnUnresolvedConflict.Should().BeTrue();
    }

    [Fact]
    public void AddEricksonLopezConcurrency_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddEricksonLopezConcurrency();
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }

    [Fact]
    public void AddConflictResolver_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddConflictResolver<SampleEntity, SampleResolver>();
        var ex = act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services").Which;

        (ex.StackTrace != null && !ex.StackTrace.Contains("ServiceCollectionServiceExtensions.AddScoped")).Should().BeTrue();
    }

    [Fact]
    public void AddEricksonLopezConcurrency_ShouldRegisterCoreServices()
    {
        var services = new ServiceCollection();
        services.AddEricksonLopezConcurrency(opt =>
        {
            opt.DefaultResolutionStrategy = ConflictResolutionStrategy.LastWriteWinsExplicit;
            opt.EnableDiagnostics = false;
            opt.ThrowOnUnresolvedConflict = true;
        });

        using ServiceProvider sp = services.BuildServiceProvider();
        var checker = sp.GetService<IConcurrencyChecker>();
        var controller = sp.GetService<IConcurrencyController>();
        var resolver = sp.GetService<IConcurrencyConflictResolver<SampleEntity>>();
        var options = sp.GetService<ConcurrencyOptions>();

        checker.Should().NotBeNull();
        controller.Should().NotBeNull();
        resolver.Should().NotBeNull();
        resolver.Should().BeOfType<RejectConflictResolver<SampleEntity>>();
        options.Should().NotBeNull();
        options!.DefaultResolutionStrategy.Should().Be(ConflictResolutionStrategy.LastWriteWinsExplicit);
        options.EnableDiagnostics.Should().BeFalse();
        options.ThrowOnUnresolvedConflict.Should().BeTrue();
    }

    [Fact]
    public void AddEricksonLopezConcurrency_WithoutConfigureAction_ShouldRegisterDefaultOptions()
    {
        var services = new ServiceCollection();
        services.AddEricksonLopezConcurrency();

        using ServiceProvider sp = services.BuildServiceProvider();
        var options = sp.GetService<ConcurrencyOptions>();
        options.Should().NotBeNull();
        options!.DefaultResolutionStrategy.Should().Be(ConflictResolutionStrategy.Reject);
    }

    [Fact]
    public void AddConflictResolver_ShouldRegisterCustomResolver()
    {
        var services = new ServiceCollection();
        services.AddEricksonLopezConcurrency();
        services.AddConflictResolver<SampleEntity, SampleResolver>();

        using ServiceProvider sp = services.BuildServiceProvider();
        var resolver = sp.GetService<IConcurrencyConflictResolver<SampleEntity>>();

        resolver.Should().NotBeNull();
        resolver.Should().BeOfType<SampleResolver>();
    }
}
