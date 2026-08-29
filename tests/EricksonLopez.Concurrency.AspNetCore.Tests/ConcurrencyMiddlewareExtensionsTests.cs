// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.AspNetCore.Tests;

public sealed class ConcurrencyMiddlewareExtensionsTests
{
    [Fact]
    public void UseConcurrencyConflictHandling_ValidApp_ShouldReturnApp()
    {
        var services = new ServiceCollection();
        IServiceProvider serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);

        IApplicationBuilder result = app.UseConcurrencyConflictHandling();

        result.Should().BeSameAs(app);
    }

    [Fact]
    public void UseConcurrencyConflictHandling_NullApp_ShouldThrowArgumentNullException()
    {
        IApplicationBuilder nullApp = null!;
        Action act = () => nullApp.UseConcurrencyConflictHandling();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("app");
    }
}
