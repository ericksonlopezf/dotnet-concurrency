// Copyright © Erickson Lopez. MIT License.
using System;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ExpectedVersionTests
{
    [Fact]
    public void ExpectedVersion_Any_ShouldMatchAnyVersion()
    {
        var expected = ExpectedVersion.Any;

        expected.Matches(ConcurrencyVersion.None).Should().BeTrue();
        expected.Matches(new ConcurrencyVersion(1)).Should().BeTrue();
        expected.Matches(new ConcurrencyVersion(999)).Should().BeTrue();
        expected.Kind.Should().Be(ExpectedVersionKind.Any);
        expected.ToString().Should().Be("[Expected:Any]");
    }

    [Fact]
    public void ExpectedVersion_New_ShouldOnlyMatchNone()
    {
        var expected = ExpectedVersion.New;

        expected.Matches(ConcurrencyVersion.None).Should().BeTrue();
        expected.Matches(new ConcurrencyVersion(1)).Should().BeFalse();
        expected.Matches(new ConcurrencyVersion(100)).Should().BeFalse();
        expected.Kind.Should().Be(ExpectedVersionKind.New);
        expected.ToString().Should().Be("[Expected:New]");
    }

    [Fact]
    public void ExpectedVersion_Exists_ShouldMatchAnyNonZeroVersion()
    {
        var expected = ExpectedVersion.Exists;

        expected.Matches(ConcurrencyVersion.None).Should().BeFalse();
        expected.Matches(new ConcurrencyVersion(1)).Should().BeTrue();
        expected.Matches(new ConcurrencyVersion(50)).Should().BeTrue();
        expected.Kind.Should().Be(ExpectedVersionKind.Exists);
        expected.ToString().Should().Be("[Expected:Exists]");
    }

    [Fact]
    public void ExpectedVersion_Specific_ShouldMatchOnlyExactVersion()
    {
        var expectedFromLong = ExpectedVersion.Specific(17);
        var expectedFromCv = ExpectedVersion.Specific(new ConcurrencyVersion(17));

        expectedFromLong.Matches(new ConcurrencyVersion(17)).Should().BeTrue();
        expectedFromLong.Matches(new ConcurrencyVersion(16)).Should().BeFalse();
        expectedFromLong.Matches(new ConcurrencyVersion(18)).Should().BeFalse();
        expectedFromLong.Kind.Should().Be(ExpectedVersionKind.Specific);
        expectedFromLong.Version.Value.Should().Be(17);
        expectedFromLong.ToString().Should().Be("[Expected:17]");

        expectedFromCv.Should().Be(expectedFromLong);
    }

    [Fact]
    public void ExpectedVersion_ImplicitConversions_ShouldWork()
    {
        ExpectedVersion fromCv = new ConcurrencyVersion(42);
        ExpectedVersion fromLong = 42L;

        fromCv.Kind.Should().Be(ExpectedVersionKind.Specific);
        fromCv.Version.Value.Should().Be(42);

        fromLong.Kind.Should().Be(ExpectedVersionKind.Specific);
        fromLong.Version.Value.Should().Be(42);

        fromCv.Should().Be(fromLong);
    }

    [Fact]
    public void ExpectedVersion_CompareTo_Generic_ShouldCompareByKindThenVersion()
    {
        var specific10 = ExpectedVersion.Specific(10);
        var specific20 = ExpectedVersion.Specific(20);
        var specific10Copy = ExpectedVersion.Specific(10);
        var any = ExpectedVersion.Any;
        var newV = ExpectedVersion.New;
        var exists = ExpectedVersion.Exists;

        // Specific kind is 0, Any is 1, New is 2, Exists is 3
        specific10.CompareTo(any).Should().BeLessThan(0);
        any.CompareTo(newV).Should().BeLessThan(0);
        newV.CompareTo(exists).Should().BeLessThan(0);

        specific10.CompareTo(specific20).Should().BeLessThan(0);
        specific20.CompareTo(specific10).Should().BeGreaterThan(0);
        specific10.CompareTo(specific10Copy).Should().Be(0);
    }

    [Fact]
    public void ExpectedVersion_CompareTo_Object_ShouldHandleNullAndTypeValidation()
    {
        var expected = ExpectedVersion.Specific(10);

        expected.CompareTo(null).Should().Be(1);
        expected.CompareTo((object)ExpectedVersion.Specific(20)).Should().BeLessThan(0);
        expected.CompareTo((object)ExpectedVersion.Specific(10)).Should().Be(0);

        Action act = () => expected.CompareTo("not_expected_version");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ExpectedVersion.*")
            .WithParameterName("obj");
    }

    [Fact]
    public void ExpectedVersion_ComparisonOperators_ShouldEvaluateCorrectly()
    {
        var v5 = ExpectedVersion.Specific(5);
        var v10 = ExpectedVersion.Specific(10);
        var v10Copy = ExpectedVersion.Specific(10);

        (v5 < v10).Should().BeTrue();
        (v10 < v5).Should().BeFalse();
        (v10 < v10Copy).Should().BeFalse(); // Kills < mutated to <=

        (v5 <= v10).Should().BeTrue();
        (v10 <= v10Copy).Should().BeTrue();
        (v10 <= v5).Should().BeFalse();

        (v10 > v5).Should().BeTrue();
        (v5 > v10).Should().BeFalse();
        (v10 > v10Copy).Should().BeFalse(); // Kills > mutated to >=

        (v10 >= v5).Should().BeTrue();
        (v10 >= v10Copy).Should().BeTrue();
        (v5 >= v10).Should().BeFalse();
    }

    [Fact]
    public void ExpectedVersion_InvalidKind_MatchesAndToStringFallback()
    {
        // Construct invalid enum value via reflection private constructor
        var ctor = typeof(ExpectedVersion).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            [typeof(long), typeof(ExpectedVersionKind)],
            null);

        var invalidExpected = (ExpectedVersion)ctor!.Invoke([0L, (ExpectedVersionKind)99]);
        invalidExpected.Matches(new ConcurrencyVersion(10)).Should().BeFalse();
        invalidExpected.ToString().Should().Be("[Expected:99]");
    }

    [Fact]
    public void ExpectedVersionKind_EnumValues()
    {
        ((byte)ExpectedVersionKind.Specific).Should().Be(0);
        ((byte)ExpectedVersionKind.Any).Should().Be(1);
        ((byte)ExpectedVersionKind.New).Should().Be(2);
        ((byte)ExpectedVersionKind.Exists).Should().Be(3);
    }
}
