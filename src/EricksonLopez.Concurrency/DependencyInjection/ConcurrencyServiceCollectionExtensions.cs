// Copyright © Erickson Lopez. MIT License.
using System;
using System.Diagnostics.CodeAnalysis;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Resolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EricksonLopez.Concurrency.DependencyInjection;

/// <summary>
/// Provides extension methods for registering concurrency services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ConcurrencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers core concurrency services including the default concurrency checker, controller, and rejection resolver.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">An optional action delegate to configure <see cref="ConcurrencyOptions"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    public static IServiceCollection AddEricksonLopezConcurrency(
        this IServiceCollection services,
        Action<ConcurrencyOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new ConcurrencyOptions();
        configure?.Invoke(options);

        services.TryAddSingleton(options);
        services.TryAddSingleton<IConcurrencyChecker>(OptimisticConcurrencyChecker.Instance);
        services.TryAddSingleton<IConcurrencyController, ConcurrencyController>();
        services.TryAddTransient(typeof(IConcurrencyConflictResolver<>), typeof(RejectConflictResolver<>));

        return services;
    }

    /// <summary>
    /// Registers a custom domain-specific conflict resolver for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The type of the domain entity or aggregate root.</typeparam>
    /// <typeparam name="TResolver">The type of the custom conflict resolver implementation.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the resolver to.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    public static IServiceCollection AddConflictResolver<TEntity, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TResolver>(this IServiceCollection services)
        where TEntity : class
        where TResolver : class, IConcurrencyConflictResolver<TEntity>
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IConcurrencyConflictResolver<TEntity>, TResolver>();
        return services;
    }
}
