// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.MySql.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.MySql.Tests;

public sealed class MySqlConcurrencyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEricksonLopezConcurrencyMySql_ValidServices_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddEricksonLopezConcurrencyMySql();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEricksonLopezConcurrencyMySql_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddEricksonLopezConcurrencyMySql();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
