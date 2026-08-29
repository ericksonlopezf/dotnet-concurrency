// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.AspNetCore.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.AspNetCore.Tests;

public sealed class ConcurrencyAspNetCoreServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConcurrencyAspNetCore_ValidServices_ShouldRegisterProblemDetailsAndReturnServices()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddConcurrencyAspNetCore();

        result.Should().BeSameAs(services);
        services.Should().Contain(sd => sd.ServiceType == typeof(Microsoft.AspNetCore.Http.IProblemDetailsService));
    }

    [Fact]
    public void AddConcurrencyAspNetCore_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddConcurrencyAspNetCore();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
