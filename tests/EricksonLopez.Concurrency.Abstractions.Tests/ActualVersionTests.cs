// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using Xunit;

namespace EricksonLopez.Concurrency.Abstractions.Tests;

public sealed class ActualVersionTests
{
    [Fact]
    public void NotFound_ShouldHaveExistsFalseAndNoneVersion()
    {
        var actual = ActualVersion.NotFound;

        actual.Exists.Should().BeFalse();
        actual.Version.Should().Be(ConcurrencyVersion.None);
        actual.ToString().Should().Be("[Actual:NotFound]");
    }

    [Fact]
    public void From_WithNumericValue_ShouldHaveExistsTrue()
    {
        var actual = ActualVersion.From(42);

        actual.Exists.Should().BeTrue();
        actual.Version.Value.Should().Be(42);
        actual.ToString().Should().Be("[Actual:42]");
    }

    [Fact]
    public void From_WithConcurrencyVersion_ShouldHaveExistsTrue()
    {
        var cv = new ConcurrencyVersion(99);
        var actual = ActualVersion.From(cv);

        actual.Exists.Should().BeTrue();
        actual.Version.Should().Be(cv);
        actual.ToString().Should().Be("[Actual:99]");
    }

    [Fact]
    public void Constructors_ShouldInitializePropertiesCorrectly()
    {
        var customFalse = new ActualVersion(new ConcurrencyVersion(10), exists: false);
        customFalse.Exists.Should().BeFalse();
        customFalse.Version.Value.Should().Be(10);
        customFalse.ToString().Should().Be("[Actual:NotFound]");

        var customLong = new ActualVersion(15, exists: true);
        customLong.Exists.Should().BeTrue();
        customLong.Version.Value.Should().Be(15);
    }

    [Fact]
    public void ImplicitConversions_ShouldWorkSeamlessly()
    {
        ActualVersion fromCv = new ConcurrencyVersion(50);
        ActualVersion fromLong = 100L;

        fromCv.Exists.Should().BeTrue();
        fromCv.Version.Value.Should().Be(50);

        fromLong.Exists.Should().BeTrue();
        fromLong.Version.Value.Should().Be(100);
    }

    [Fact]
    public void CompareTo_Generic_ShouldCompareByExistsThenVersion()
    {
        var notFound = ActualVersion.NotFound;
        var v10 = ActualVersion.From(10);
        var v20 = ActualVersion.From(20);
        var v10b = ActualVersion.From(10);

        // When Exists differs, it MUST take precedence over Version
        var notFoundBigVersion = new ActualVersion(20, exists: false);
        var existsSmallVersion = new ActualVersion(10, exists: true);
        notFoundBigVersion.CompareTo(existsSmallVersion).Should().BeLessThan(0);
        existsSmallVersion.CompareTo(notFoundBigVersion).Should().BeGreaterThan(0);

        notFound.CompareTo(v10).Should().BeLessThan(0);
        v10.CompareTo(notFound).Should().BeGreaterThan(0);

        v10.CompareTo(v20).Should().BeLessThan(0);
        v20.CompareTo(v10).Should().BeGreaterThan(0);
        v10.CompareTo(v10b).Should().Be(0);
    }

    [Fact]
    public void CompareTo_Object_ShouldHandleNullAndTypes()
    {
        var actual = ActualVersion.From(10);

        actual.CompareTo(null).Should().Be(1);
        actual.CompareTo((object)ActualVersion.From(20)).Should().BeLessThan(0);
        actual.CompareTo((object)ActualVersion.From(10)).Should().Be(0);

        Action act = () => actual.CompareTo("invalid_type");
        act.Should().Throw<ArgumentException>()
            .WithMessage("Object must be of type ActualVersion.*")
            .WithParameterName("obj");
    }

    [Fact]
    public void ComparisonOperators_ShouldEvaluateCorrectly()
    {
        var v5 = ActualVersion.From(5);
        var v10 = ActualVersion.From(10);
        var v10Copy = ActualVersion.From(10);

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
}
