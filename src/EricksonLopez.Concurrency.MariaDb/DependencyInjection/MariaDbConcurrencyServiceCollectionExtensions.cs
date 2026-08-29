// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.MariaDb.DependencyInjection;

/// <summary>
/// Provides extension methods for registering MariaDB concurrency services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class MariaDbConcurrencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers MariaDB concurrency dialect services into the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddEricksonLopezConcurrencyMariaDb(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
