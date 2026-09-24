// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Controllers;
using Xunit;

namespace EricksonLopez.Concurrency.Tests;

public sealed class ControllersInternalTests
{
    [Fact]
    public void RefCountedLock_Reset_ShouldSetRefCountToZero()
    {
        var lockObj = new RefCountedLock();
        lockObj.RefCount.Should().Be(1);

        lockObj.RefCount = 10;
        lockObj.Reset();

        lockObj.RefCount.Should().Be(0);
    }

    [Fact]
    public void RefCountedLock_Dispose_ShouldDisposeUnderlyingSemaphore()
    {
        var lockObj = new RefCountedLock();
        lockObj.Dispose();

        Action act = () => lockObj.Semaphore.Release();
        act.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void ReentrancyNode_Contains_ShouldReturnTrueWhenPresentAndFalseWhenAbsent()
    {
        var root = new ReentrancyNode("entity-1", null);
        var child = new ReentrancyNode("entity-2", root);

        child.Contains("entity-2").Should().BeTrue();
        child.Contains("entity-1").Should().BeTrue();
        child.Contains("entity-3").Should().BeFalse();
        root.Contains("entity-2").Should().BeFalse();
    }
}
