// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Dapper.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.Dapper.Tests;

public sealed class DapperConcurrencyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEricksonLopezConcurrencyDapper_ValidServices_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddEricksonLopezConcurrencyDapper();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEricksonLopezConcurrencyDapper_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddEricksonLopezConcurrencyDapper();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
