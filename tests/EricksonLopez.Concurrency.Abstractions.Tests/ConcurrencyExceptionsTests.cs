// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ConcurrencyExceptionsTests
{
    [Fact]
    public void ConcurrencyException_DefaultConstructor_ShouldHaveDefaultMessage()
    {
        var ex = new ConcurrencyException();

        ex.Message.Should().Be("A concurrency conflict occurred.");
        ex.Conflict.Should().BeNull();
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void ConcurrencyException_MessageConstructor_ShouldSetMessage()
    {
        var ex = new ConcurrencyException("Custom error message.");

        ex.Message.Should().Be("Custom error message.");
        ex.Conflict.Should().BeNull();
    }

    [Fact]
    public void ConcurrencyException_MessageAndInnerExceptionConstructor_ShouldSetBoth()
    {
        var inner = new InvalidOperationException("Inner failure.");
        var ex = new ConcurrencyException("Outer error.", inner);

        ex.Message.Should().Be("Outer error.");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ConcurrencyException_ConflictConstructor_ShouldSetConflictAndMessage()
    {
        var conflict = ConcurrencyConflict.Deleted("item_1", "Item");
        var ex = new ConcurrencyException(conflict);

        ex.Conflict.Should().BeSameAs(conflict);
        ex.Message.Should().Be(conflict.Message);
    }

    [Fact]
    public void ConcurrencyException_ConflictConstructor_WithNullConflict_ShouldFallbackMessage()
    {
        var ex = new ConcurrencyException((ConcurrencyConflict)null!);

        ex.Conflict.Should().BeNull();
        ex.Message.Should().Be("A concurrency conflict occurred.");
    }

    [Fact]
    public void ConcurrencyConfigurationException_Constructors_ShouldWorkProperly()
    {
        var exDefault = new ConcurrencyConfigurationException();
        exDefault.Message.Should().Be("Invalid concurrency configuration.");

        var exMsg = new ConcurrencyConfigurationException("Missing resolver.");
        exMsg.Message.Should().Be("Missing resolver.");

        var inner = new ArgumentException("Bad setting");
        var exInner = new ConcurrencyConfigurationException("Config failed.", inner);
        exInner.Message.Should().Be("Config failed.");
        exInner.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void ConcurrencyTokenMismatchException_Constructors_ShouldWorkProperly()
    {
        var exDefault = new ConcurrencyTokenMismatchException();
        exDefault.Message.Should().Be("Concurrency token mismatch.");

        var exMsg = new ConcurrencyTokenMismatchException("Tokens did not match.");
        exMsg.Message.Should().Be("Tokens did not match.");

        var inner = new InvalidOperationException("Root cause");
        var exInner = new ConcurrencyTokenMismatchException("Detailed mismatch.", inner);
        exInner.Message.Should().Be("Detailed mismatch.");
        exInner.InnerException.Should().BeSameAs(inner);

        var token1 = new ConcurrencyToken("token-1", "Kind1");
        var token2 = new ConcurrencyToken("token-2", "Kind2");
        var exTokens = new ConcurrencyTokenMismatchException(token1, token2);

        exTokens.ExpectedToken.Should().Be(token1);
        exTokens.ActualToken.Should().Be(token2);
        exTokens.Message.Should().Be("Concurrency token mismatch. Expected: 'token-1', Actual: 'token-2'.");

        var exTokensNull = new ConcurrencyTokenMismatchException((IConcurrencyToken)null!, (IConcurrencyToken)null!);
        exTokensNull.ExpectedToken.Should().BeNull();
        exTokensNull.ActualToken.Should().BeNull();
        exTokensNull.Message.Should().Be("Concurrency token mismatch. Expected: '', Actual: ''.");
    }
}
