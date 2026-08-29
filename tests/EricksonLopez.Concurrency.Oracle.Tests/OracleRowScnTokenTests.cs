// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Oracle;
using Xunit;

namespace EricksonLopez.Concurrency.Oracle.Tests;

public sealed class OracleRowScnTokenTests
{
    private sealed class CustomTokenStub : IConcurrencyToken
    {
        public string Value { get; init; } = string.Empty;
        public string TokenKind { get; init; } = string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void OracleRowScnToken_ConstructorAndProperties_ShouldInitializeProperly()
    {
        var token = new OracleRowScnToken(1234567890L);
        token.RowScn.Should().Be(1234567890L);
        token.Value.Should().Be("1234567890");
        token.TokenKind.Should().Be("Oracle.ORA_ROWSCN");
        token.IsEmpty.Should().BeFalse();
        token.ToString().Should().Be("[ORA_ROWSCN:1234567890]");

        var emptyToken = new OracleRowScnToken(0);
        emptyToken.RowScn.Should().Be(0);
        emptyToken.Value.Should().Be("0");
        emptyToken.IsEmpty.Should().BeTrue();
        emptyToken.ToString().Should().Be("[ORA_ROWSCN:0]");

        var noneToken = OracleRowScnToken.None;
        noneToken.RowScn.Should().Be(0);
        noneToken.IsEmpty.Should().BeTrue();

        var defaultToken = default(OracleRowScnToken);
        defaultToken.RowScn.Should().Be(0);
        defaultToken.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void OracleRowScnToken_NegativeScn_ShouldThrowArgumentOutOfRangeException()
    {
        Action act = () => _ = new OracleRowScnToken(-1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("rowScn");
    }

    [Fact]
    public void Parse_ValidString_ShouldReturnToken()
    {
        var token = OracleRowScnToken.Parse("987654321");
        token.RowScn.Should().Be(987654321L);
        token.Value.Should().Be("987654321");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhiteSpace_ShouldThrowArgumentException(string? scn)
    {
        Action act = () => OracleRowScnToken.Parse(scn!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("scnString");
    }

    [Fact]
    public void Equals_WithIConcurrencyTokenAndTyped_ShouldEvaluateAllBranches()
    {
        var t1 = new OracleRowScnToken(100);
        var t2 = new OracleRowScnToken(100);
        var t3 = new OracleRowScnToken(200);

        t1.Equals((IConcurrencyToken?)null).Should().BeFalse();
        t1.Equals((IConcurrencyToken)t2).Should().BeTrue();
        t1.Equals((IConcurrencyToken)t3).Should().BeFalse();

        var customStub = new CustomTokenStub { Value = "100" };
        t1.Equals((IConcurrencyToken)customStub).Should().BeTrue();

        t1.Equals(t2).Should().BeTrue();
        t1.Equals(t3).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_AndOperators_ShouldWorkAccurately()
    {
        var t1 = new OracleRowScnToken(100);
        var t2 = new OracleRowScnToken(100);
        var t3 = new OracleRowScnToken(200);

        t1.CompareTo(t2).Should().Be(0);
        t1.CompareTo(t3).Should().BeLessThan(0);
        t3.CompareTo(t1).Should().BeGreaterThan(0);

        t1.CompareTo((object?)null).Should().Be(1);
        t1.CompareTo((object)t2).Should().Be(0);

        Action act = () => t1.CompareTo("invalid");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("obj")
            .WithMessage("*Object must be of type OracleRowScnToken.*");

        (t1 == t2).Should().BeTrue();
        (t1 == t3).Should().BeFalse();
        (t1 != t3).Should().BeTrue();
        (t1 != t2).Should().BeFalse();

        (t1 < t3).Should().BeTrue();
        (t3 < t1).Should().BeFalse();
        (t1 < t2).Should().BeFalse();
        (t2 < t1).Should().BeFalse();

        (t1 <= t3).Should().BeTrue();
        (t1 <= t2).Should().BeTrue();
        (t3 <= t1).Should().BeFalse();

        (t3 > t1).Should().BeTrue();
        (t1 > t3).Should().BeFalse();
        (t1 > t2).Should().BeFalse();
        (t2 > t1).Should().BeFalse();

        (t3 >= t1).Should().BeTrue();
        (t1 >= t2).Should().BeTrue();
        (t1 >= t3).Should().BeFalse();
    }
}
