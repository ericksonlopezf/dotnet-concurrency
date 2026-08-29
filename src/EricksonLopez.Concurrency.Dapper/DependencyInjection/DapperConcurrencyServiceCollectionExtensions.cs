// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.Dapper.DependencyInjection;

/// <summary>
/// Provides extension methods for registering Dapper concurrency adapters into an <see cref="IServiceCollection"/>.
/// </summary>
public static class DapperConcurrencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers Dapper optimistic concurrency adapter services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddEricksonLopezConcurrencyDapper(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
