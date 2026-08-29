// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Oracle.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.Oracle.Tests;

public sealed class OracleConcurrencyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEricksonLopezConcurrencyOracle_ValidServices_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddEricksonLopezConcurrencyOracle();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEricksonLopezConcurrencyOracle_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddEricksonLopezConcurrencyOracle();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
