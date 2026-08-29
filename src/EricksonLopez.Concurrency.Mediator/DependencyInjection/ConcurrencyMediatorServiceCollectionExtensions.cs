// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.Mediator.DependencyInjection;

/// <summary>
/// Provides extension methods for registering the mediator concurrency pipeline behavior into an <see cref="IServiceCollection"/>.
/// </summary>
public static class ConcurrencyMediatorServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="ConcurrencyBehavior{TRequest, TResponse}"/> open generic pipeline behavior in the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    public static IServiceCollection AddConcurrencyMediatorBehavior(this IServiceCollection services) =>
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ConcurrencyBehavior<,>));
}
