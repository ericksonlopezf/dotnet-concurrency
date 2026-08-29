// Copyright © Erickson Lopez. MIT License.
using System;
using System.Linq;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Concurrency.Mediator.DependencyInjection;
using EricksonLopez.Mediator;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EricksonLopez.Concurrency.Mediator.Tests;

public sealed class ConcurrencyMediatorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConcurrencyMediatorBehavior_ValidServices_ShouldRegisterBehavior()
    {
        var services = new ServiceCollection();
        IServiceCollection result = services.AddConcurrencyMediatorBehavior();

        result.Should().BeSameAs(services);

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IPipelineBehavior<,>));
        descriptor.Should().NotBeNull();
        descriptor!.ImplementationType.Should().Be(typeof(ConcurrencyBehavior<,>));
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    [Fact]
    public void AddConcurrencyMediatorBehavior_NullServices_ShouldThrowArgumentNullException()
    {
        IServiceCollection nullServices = null!;
        Action act = () => nullServices.AddConcurrencyMediatorBehavior();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("services");
    }
}
