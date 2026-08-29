// Copyright © Erickson Lopez. MIT License.
using System;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.PostgreSql.DependencyInjection;

/// <summary>
/// Provides extension methods for registering PostgreSQL concurrency services into an <see cref="IServiceCollection"/>.
/// </summary>
public static class PostgreSqlConcurrencyServiceCollectionExtensions
{
    /// <summary>
    /// Registers PostgreSQL concurrency adapter services and diagnostic classifiers.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/></exception>
    public static IServiceCollection AddEricksonLopezConcurrencyPostgreSql(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services;
    }
}
