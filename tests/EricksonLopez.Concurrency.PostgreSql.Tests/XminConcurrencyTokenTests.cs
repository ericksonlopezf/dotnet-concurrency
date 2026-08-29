// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.PostgreSql;
using Xunit;

namespace EricksonLopez.Concurrency.PostgreSql.Tests;

public sealed class XminConcurrencyTokenTests
{
    private sealed class CustomTokenStub : IConcurrencyToken
    {
        public string Value { get; init; } = string.Empty;
        public string TokenKind { get; init; } = string.Empty;
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value && TokenKind == other.TokenKind;
    }

    [Fact]
    public void XminConcurrencyToken_ShouldInitializeAndFormatCorrectly()
    {
        var token = new XminConcurrencyToken(1234567);

        token.Xmin.Should().Be(1234567);
        token.Value.Should().Be("1234567");
        token.TokenKind.Should().Be("PostgreSql.xmin");
        token.IsEmpty.Should().BeFalse();
        token.ToString().Should().Be("[xmin:1234567]");
    }

    [Fact]
    public void XminConcurrencyToken_FromFactory_ShouldCreateInstance()
    {
        XminConcurrencyToken token = XminConcurrencyToken.From(42);
        token.Xmin.Should().Be(42);
        token.IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void XminConcurrencyToken_None_ShouldBeEmpty()
    {
        var token = XminConcurrencyToken.None;

        token.Xmin.Should().Be(0);
        token.IsEmpty.Should().BeTrue();
        token.ToString().Should().Be("[xmin:0]");
    }

    [Fact]
    public void Parse_ValidString_ShouldParseCorrectly()
    {
        XminConcurrencyToken token = XminConcurrencyToken.Parse("987654");
        token.Xmin.Should().Be(987654);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_NullOrWhiteSpace_ShouldThrowArgumentException(string? input)
    {
        Action act = () => XminConcurrencyToken.Parse(input!);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Parse_InvalidNumeric_ShouldThrowFormatException()
    {
        Action act = () => XminConcurrencyToken.Parse("not-a-number");
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Equals_WithIConcurrencyToken_ShouldEvaluateAllBranches()
    {
        var token = new XminConcurrencyToken(100);

        token.Equals((IConcurrencyToken?)null).Should().BeFalse();

        var sameXmin = new XminConcurrencyToken(100);
        var diffXmin = new XminConcurrencyToken(200);
        token.Equals((IConcurrencyToken)sameXmin).Should().BeTrue();
        token.Equals((IConcurrencyToken)diffXmin).Should().BeFalse();

        var customMatching = new CustomTokenStub { Value = "100", TokenKind = "PostgreSql.xmin" };
        var customDiffVal = new CustomTokenStub { Value = "200", TokenKind = "PostgreSql.xmin" };
        var customDiffKind = new CustomTokenStub { Value = "100", TokenKind = "Other" };

        token.Equals((IConcurrencyToken)customMatching).Should().BeTrue();
        token.Equals((IConcurrencyToken)customDiffVal).Should().BeFalse();
        token.Equals((IConcurrencyToken)customDiffKind).Should().BeFalse();
    }

    [Fact]
    public void CompareTo_Generic_ShouldCompareAccurately()
    {
        var t1 = new XminConcurrencyToken(100);
        var t2 = new XminConcurrencyToken(100);
        var t3 = new XminConcurrencyToken(200);

        t1.CompareTo(t2).Should().Be(0);
        t1.CompareTo(t3).Should().BeLessThan(0);
        t3.CompareTo(t1).Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_Object_ShouldHandleValidAndInvalidTypes()
    {
        var t1 = new XminConcurrencyToken(100);
        var t2 = new XminConcurrencyToken(200);

        t1.CompareTo((object?)null).Should().Be(1);
        t1.CompareTo((object)t2).Should().BeLessThan(0);

        Action act = () => t1.CompareTo("invalid-object");
        act.Should().Throw<ArgumentException>()
            .WithParameterName("obj")
            .WithMessage($"*Object must be of type {nameof(XminConcurrencyToken)}*");
    }

    [Fact]
    public void RelationalOperators_ShouldWorkAccurately()
    {
        var t1 = new XminConcurrencyToken(100);
        var t2 = new XminConcurrencyToken(100);
        var t3 = new XminConcurrencyToken(200);

        (t1 < t3).Should().BeTrue();
        (t3 < t1).Should().BeFalse();
        (t1 < t2).Should().BeFalse();

        (t1 <= t3).Should().BeTrue();
        (t1 <= t2).Should().BeTrue();
        (t3 <= t1).Should().BeFalse();

        (t3 > t1).Should().BeTrue();
        (t1 > t3).Should().BeFalse();
        (t1 > t2).Should().BeFalse();

        (t3 >= t1).Should().BeTrue();
        (t1 >= t2).Should().BeTrue();
        (t1 >= t3).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversions_ShouldConvertBetweenUintAndToken()
    {
        uint raw = 555;
        XminConcurrencyToken token = raw;
        token.Xmin.Should().Be(555);

        uint backToRaw = token;
        backToRaw.Should().Be(555);
    }

    [Fact]
    public void ToString_WithFormatAndProvider_ShouldFormatProperly()
    {
        var token = new XminConcurrencyToken(255);
        string hex = token.ToString("X", CultureInfo.InvariantCulture);
        hex.Should().Be("FF");
    }

    [Fact]
    public void TryFormat_ShouldFormatToSpan()
    {
        var token = new XminConcurrencyToken(12345);
        Span<char> buffer = stackalloc char[10];

        bool success = token.TryFormat(buffer, out int charsWritten, default, CultureInfo.InvariantCulture);
        success.Should().BeTrue();
        charsWritten.Should().Be(5);
        new string(buffer[..charsWritten]).Should().Be("12345");

        Span<char> tinyBuffer = stackalloc char[2];
        bool failed = token.TryFormat(tinyBuffer, out _, default, CultureInfo.InvariantCulture);
        failed.Should().BeFalse();
    }
}
