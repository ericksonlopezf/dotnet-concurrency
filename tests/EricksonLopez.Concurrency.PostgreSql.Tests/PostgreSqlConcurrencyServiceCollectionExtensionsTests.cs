// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.PostgreSql.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.PostgreSql.Tests;

public sealed class PostgreSqlConcurrencyServiceCollectionExtensionsTests
{
    [Fact]
    public void AddEricksonLopezConcurrencyPostgreSql_ValidServices_ShouldReturnSameCollection()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddEricksonLopezConcurrencyPostgreSql();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddEricksonLopezConcurrencyPostgreSql_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddEricksonLopezConcurrencyPostgreSql();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
