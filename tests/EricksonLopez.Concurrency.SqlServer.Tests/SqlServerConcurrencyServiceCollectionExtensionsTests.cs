// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.SqlServer.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.SqlServer.Tests;

public sealed class SqlServerConcurrencyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEricksonLopezConcurrencySqlServer_ValidServices_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddEricksonLopezConcurrencySqlServer();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEricksonLopezConcurrencySqlServer_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddEricksonLopezConcurrencySqlServer();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
