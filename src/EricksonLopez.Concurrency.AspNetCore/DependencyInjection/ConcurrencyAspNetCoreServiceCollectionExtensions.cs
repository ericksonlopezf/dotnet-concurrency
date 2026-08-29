// Copyright © Erickson Lopez. MIT License.
using System;
using EricksonLopez.Concurrency.AspNetCore.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace EricksonLopez.Concurrency.AspNetCore.DependencyInjection;

/// <summary>
/// Provides extension methods for registering ASP.NET Core concurrency integration services.
/// </summary>
public static class ConcurrencyAspNetCoreServiceCollectionExtensions
{
    /// <summary>
    /// Registers ASP.NET Core concurrency integration dependencies into the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The <see cref="IServiceCollection"/> instance so that calls can be chained.</returns>
    public static IServiceCollection AddConcurrencyAspNetCore(this IServiceCollection services) =>
        services.AddProblemDetails();
}
