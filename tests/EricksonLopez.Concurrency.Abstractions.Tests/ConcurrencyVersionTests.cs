// Copyright © Erickson Lopez. MIT License.
using System;
using System.Globalization;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ConcurrencyVersionTests
{
    private sealed class CustomerAggregate
    {
    }

    private sealed class OrderAggregate
    {
    }

    [Fact]
    public void ConcurrencyVersion_ShouldInitializeCorrectly_WithValidValues()
    {
        var v0 = ConcurrencyVersion.None;
        var v1 = ConcurrencyVersion.Initial;
        var v10 = ConcurrencyVersion.From(10);

        v0.Value.Should().Be(0);
        v0.IsNone.Should().BeTrue();

        v1.Value.Should().Be(1);
        v1.IsNone.Should().BeFalse();

        v10.Value.Should().Be(10);
        v10.IsNone.Should().BeFalse();
    }

    [Fact]
    public void ConcurrencyVersion_ShouldThrow_WhenValueIsNegative()
    {
        Action act = () => _ = new ConcurrencyVersion(-1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Concurrency version value must be non-negative.*")
            .WithParameterName("value");

        Action actFrom = () => _ = ConcurrencyVersion.From(-10);
        actFrom.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");

        Action actExplicit = () => _ = (ConcurrencyVersion)(-5);
        actExplicit.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    [Fact]
    public void ConcurrencyVersion_Next_ShouldIncrementVersionByOne()
    {
        var version = new ConcurrencyVersion(10);
        ConcurrencyVersion next = version.Next();

        next.Value.Should().Be(11);
    }

    [Fact]
    public void ConcurrencyVersion_Next_ShouldThrowOverflow_AtLongMaxValue()
    {
        var maxVersion = new ConcurrencyVersion(long.MaxValue);
        Action act = () => maxVersion.Next();
        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void ConcurrencyVersion_Comparisons_ShouldWorkAccurately()
    {
        var v1 = new ConcurrencyVersion(10);
        var v2 = new ConcurrencyVersion(20);
        var v3 = new ConcurrencyVersion(10);

        (v1 < v2).Should().BeTrue();
        (v2 < v1).Should().BeFalse();
        (v1 < v3).Should().BeFalse(); // Kills < mutated to <=

        (v1 <= v2).Should().BeTrue();
        (v1 <= v3).Should().BeTrue();
        (v2 <= v1).Should().BeFalse();

        (v2 > v1).Should().BeTrue();
        (v1 > v2).Should().BeFalse();
        (v1 > v3).Should().BeFalse(); // Kills > mutated to >=

        (v2 >= v1).Should().BeTrue();
        (v1 >= v3).Should().BeTrue();
        (v1 >= v2).Should().BeFalse();

        (v1 == v3).Should().BeTrue();
        (v1 != v2).Should().BeTrue();

        v1.CompareTo(v2).Should().BeLessThan(0);
        v2.CompareTo(v1).Should().BeGreaterThan(0);
        v1.CompareTo(v3).Should().Be(0);

        v1.CompareTo(null).Should().Be(1);
        v1.CompareTo((object)v2).Should().BeLessThan(0);
        v1.CompareTo((object)v3).Should().Be(0);

        Action act = () => v1.CompareTo("invalid_type");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ConcurrencyVersion.*")
            .WithParameterName("obj");
    }

    [Fact]
    public void ConcurrencyVersion_ImplicitAndExplicitConversions_ShouldWork()
    {
        var version = new ConcurrencyVersion(42);
        long raw = version;
        var reconstructed = (ConcurrencyVersion)raw;

        raw.Should().Be(42);
        reconstructed.Should().Be(version);
    }

    [Fact]
    public void ConcurrencyVersion_ToString_AndFormatting_ShouldWork()
    {
        var version = new ConcurrencyVersion(100);
        version.ToString().Should().Be("100");
        version.ToString("D5", CultureInfo.InvariantCulture).Should().Be("00100");

        Span<char> buffer = stackalloc char[10];
        bool formatted = version.TryFormat(buffer, out int charsWritten, default, null);
        formatted.Should().BeTrue();
        new string(buffer[..charsWritten]).Should().Be("100");

        Span<char> tinyBuffer = stackalloc char[1];
        bool formatFailed = version.TryFormat(tinyBuffer, out _, default, null);
        formatFailed.Should().BeFalse();
    }

    private sealed class TrackingFormatProvider : IFormatProvider
    {
        public bool WasCalled { get; private set; }

        public object? GetFormat(Type? formatType)
        {
            WasCalled = true;
            return CultureInfo.InvariantCulture.GetFormat(formatType);
        }
    }

    [Theory]
    [InlineData("0", 0)]
    [InlineData("1", 1)]
    [InlineData("42", 42)]
    [InlineData("9223372036854775807", 9223372036854775807)]
    public void ConcurrencyVersion_TryParse_ShouldSucceed_ForValidInputs(string input, long expected)
    {
        var trackingProvider = new TrackingFormatProvider();

        bool successSpan = ConcurrencyVersion.TryParse(input.AsSpan(), out ConcurrencyVersion resultSpan);
        bool successSpanProvider = ConcurrencyVersion.TryParse(input.AsSpan(), trackingProvider, out ConcurrencyVersion resultSpanProvider);
        bool successString = ConcurrencyVersion.TryParse(input, out ConcurrencyVersion resultString);
        bool successStringProvider = ConcurrencyVersion.TryParse(input, trackingProvider, out ConcurrencyVersion resultStringProvider);

        successSpan.Should().BeTrue();
        resultSpan.Value.Should().Be(expected);

        successSpanProvider.Should().BeTrue();
        resultSpanProvider.Value.Should().Be(expected);
        trackingProvider.WasCalled.Should().BeTrue();

        successString.Should().BeTrue();
        resultString.Value.Should().Be(expected);

        successStringProvider.Should().BeTrue();
        resultStringProvider.Value.Should().Be(expected);

        ConcurrencyVersion parsedSpan = ConcurrencyVersion.Parse(input.AsSpan(), trackingProvider);
        parsedSpan.Value.Should().Be(expected);

        ConcurrencyVersion parsedString = ConcurrencyVersion.Parse(input, trackingProvider);
        parsedString.Value.Should().Be(expected);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("-42")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("9999999999999999999999999999999999")]
    public void ConcurrencyVersion_TryParse_ShouldFail_ForInvalidInputs(string input)
    {
        bool successSpan = ConcurrencyVersion.TryParse(input.AsSpan(), out ConcurrencyVersion resultSpan);
        bool successString = ConcurrencyVersion.TryParse(input, out ConcurrencyVersion resultString);

        successSpan.Should().BeFalse();
        resultSpan.Should().Be(ConcurrencyVersion.None);

        successString.Should().BeFalse();
        resultString.Should().Be(ConcurrencyVersion.None);

        Action actSpan = () => ConcurrencyVersion.Parse(input.AsSpan());
        actSpan.Should().Throw<FormatException>()
            .WithMessage($"Input string '{input}' was not in a correct format for a non-negative ConcurrencyVersion.");

        Action actString = () => ConcurrencyVersion.Parse(input);
        actString.Should().Throw<FormatException>()
            .WithMessage($"Input string '{input}' was not in a correct format for a non-negative ConcurrencyVersion.");
    }

    [Fact]
    public void ConcurrencyVersion_TryParse_NullString_ShouldReturnFalse()
    {
        bool success = ConcurrencyVersion.TryParse((string?)null, out ConcurrencyVersion result);
        success.Should().BeFalse();
        result.Should().Be(ConcurrencyVersion.None);

        Action act = () => ConcurrencyVersion.Parse((string)null!);
        act.Should().Throw<ArgumentNullException>();
    }

    // Typed ConcurrencyVersion<TEntity> tests
    [Fact]
    public void TypedConcurrencyVersion_BasicPropertiesAndConstants()
    {
        var none = ConcurrencyVersion<CustomerAggregate>.None;
        none.Value.Should().Be(0);
        none.IsNone.Should().BeTrue();

        var initial = ConcurrencyVersion<CustomerAggregate>.Initial;
        initial.Value.Should().Be(1);
        initial.IsNone.Should().BeFalse();

        var v10 = new ConcurrencyVersion<CustomerAggregate>(10);
        v10.Value.Should().Be(10);
        v10.IsNone.Should().BeFalse();
    }

    [Fact]
    public void TypedConcurrencyVersion_NegativeValue_ShouldThrow()
    {
        Action act = () => _ = new ConcurrencyVersion<CustomerAggregate>(-1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Concurrency version value for CustomerAggregate must be non-negative.*")
            .WithParameterName("value");

        Action actExplicit = () => _ = (ConcurrencyVersion<CustomerAggregate>)(-5);
        actExplicit.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("value");
    }

    [Fact]
    public void TypedConcurrencyVersion_Next_ShouldIncrementAndHandleOverflow()
    {
        var v = new ConcurrencyVersion<CustomerAggregate>(10);
        v.Next().Value.Should().Be(11);

        var maxV = new ConcurrencyVersion<CustomerAggregate>(long.MaxValue);
        Action act = () => maxV.Next();
        act.Should().Throw<OverflowException>();
    }

    [Fact]
    public void TypedConcurrencyVersion_UntypedInteroperability()
    {
        var typed = new ConcurrencyVersion<CustomerAggregate>(25);
        ConcurrencyVersion untyped = typed.ToUntyped();
        untyped.Value.Should().Be(25);

        ConcurrencyVersion implicitUntyped = typed;
        implicitUntyped.Value.Should().Be(25);

        long rawLong = typed;
        rawLong.Should().Be(25);

        var typedFromLong = (ConcurrencyVersion<CustomerAggregate>)rawLong;
        typedFromLong.Should().Be(typed);

        var typedFromUntyped = new ConcurrencyVersion<CustomerAggregate>(untyped);
        typedFromUntyped.Should().Be(typed);
    }

    [Fact]
    public void TypedConcurrencyVersion_ComparisonsAndOperators()
    {
        var v1 = new ConcurrencyVersion<CustomerAggregate>(10);
        var v2 = new ConcurrencyVersion<CustomerAggregate>(20);
        var v3 = new ConcurrencyVersion<CustomerAggregate>(10);

        (v1 < v2).Should().BeTrue();
        (v2 < v1).Should().BeFalse();
        (v1 < v3).Should().BeFalse(); // Kills < mutated to <=

        (v1 <= v2).Should().BeTrue();
        (v1 <= v3).Should().BeTrue();
        (v2 <= v1).Should().BeFalse();

        (v2 > v1).Should().BeTrue();
        (v1 > v2).Should().BeFalse();
        (v1 > v3).Should().BeFalse(); // Kills > mutated to >=

        (v2 >= v1).Should().BeTrue();
        (v1 >= v3).Should().BeTrue();
        (v1 >= v2).Should().BeFalse();

        v1.CompareTo(v2).Should().BeLessThan(0);
        v2.CompareTo(v1).Should().BeGreaterThan(0);
        v1.CompareTo(v3).Should().Be(0);

        v1.CompareTo(null).Should().Be(1);
        v1.CompareTo((object)v2).Should().BeLessThan(0);
        v1.CompareTo((object)v3).Should().Be(0);

        Action act = () => v1.CompareTo(new ConcurrencyVersion<OrderAggregate>(10));
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ConcurrencyVersion*")
            .WithParameterName("obj");
    }

    [Fact]
    public void TypedConcurrencyVersion_FormattingAndParsing()
    {
        var trackingProvider = new TrackingFormatProvider();
        var v = new ConcurrencyVersion<CustomerAggregate>(88);
        v.ToString().Should().Be("88");
        v.ToString("D4", CultureInfo.InvariantCulture).Should().Be("0088");

        Span<char> buffer = stackalloc char[10];
        bool formatted = v.TryFormat(buffer, out int written, default, null);
        formatted.Should().BeTrue();
        new string(buffer[..written]).Should().Be("88");

        // Parse "0" explicitly to kill >= 0 mutated to > 0
        bool zeroParsed = ConcurrencyVersion<CustomerAggregate>.TryParse("0".AsSpan(), out var zeroResult);
        zeroParsed.Should().BeTrue();
        zeroResult.Value.Should().Be(0);

        bool spanParsed = ConcurrencyVersion<CustomerAggregate>.TryParse("88".AsSpan(), out var parsedSpan);
        spanParsed.Should().BeTrue();
        parsedSpan.Value.Should().Be(88);

        bool spanParsedProvider = ConcurrencyVersion<CustomerAggregate>.TryParse("88".AsSpan(), trackingProvider, out var parsedSpanProv);
        spanParsedProvider.Should().BeTrue();
        parsedSpanProv.Value.Should().Be(88);
        trackingProvider.WasCalled.Should().BeTrue();

        bool stringParsed = ConcurrencyVersion<CustomerAggregate>.TryParse("88", out var parsedString);
        stringParsed.Should().BeTrue();
        parsedString.Value.Should().Be(88);

        bool stringParsedProvider = ConcurrencyVersion<CustomerAggregate>.TryParse("88", trackingProvider, out var parsedStringProv);
        stringParsedProvider.Should().BeTrue();
        parsedStringProv.Value.Should().Be(88);

        var parseResult = ConcurrencyVersion<CustomerAggregate>.Parse("88", trackingProvider);
        parseResult.Value.Should().Be(88);

        var parseSpanResult = ConcurrencyVersion<CustomerAggregate>.Parse("88".AsSpan(), trackingProvider);
        parseSpanResult.Value.Should().Be(88);

        bool fail = ConcurrencyVersion<CustomerAggregate>.TryParse("invalid", out var failResult);
        fail.Should().BeFalse();
        failResult.Should().Be(ConcurrencyVersion<CustomerAggregate>.None);

        bool failNull = ConcurrencyVersion<CustomerAggregate>.TryParse((string?)null, out var failNullResult);
        failNull.Should().BeFalse();
        failNullResult.Should().Be(ConcurrencyVersion<CustomerAggregate>.None);

        Action actNull = () => ConcurrencyVersion<CustomerAggregate>.Parse((string)null!);
        actNull.Should().Throw<ArgumentNullException>();

        Action actInvalid = () => ConcurrencyVersion<CustomerAggregate>.Parse("bad_format");
        actInvalid.Should().Throw<FormatException>()
            .WithMessage("Input string 'bad_format' was not in a correct format for a non-negative ConcurrencyVersion*");

        Action actInvalidSpan = () => ConcurrencyVersion<CustomerAggregate>.Parse("bad_span".AsSpan());
        actInvalidSpan.Should().Throw<FormatException>()
            .WithMessage("Input string 'bad_span' was not in a correct format for a non-negative ConcurrencyVersion*");
    }
}
