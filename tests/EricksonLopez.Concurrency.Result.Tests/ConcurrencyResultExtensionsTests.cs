// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Result;
using ResultInstance = EricksonLopez.Result.Result;
using Xunit;

namespace EricksonLopez.Concurrency.Result.Tests;

public sealed class ConcurrencyResultExtensionsTests
{
    private sealed class OrderEntity
    {
        public string Id { get; init; } = string.Empty;
        public decimal Total { get; set; }
    }

    [Fact]
    public void ToResult_FromSuccessfulCasResult_ShouldReturnSuccessResult()
    {
        var order = new OrderEntity { Id = "o1", Total = 100 };
        CasResult<OrderEntity> cas = CasResult.Succeeded(order, new ConcurrencyVersion(2));

        Result<OrderEntity> result = cas.ToResult();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(order);
    }

    [Fact]
    public void ToResult_FromConflictedCasResult_ShouldReturnConflictResult()
    {
        var conflict = ConcurrencyConflict.VersionMismatch("o1", "OrderEntity", ExpectedVersion.Specific(1), ActualVersion.From(2));
        CasResult<OrderEntity> cas = CasResult.Conflicted<OrderEntity>(conflict);

        Result<OrderEntity> result = cas.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be(ConcurrencyErrors.VersionMismatchCode);
    }

    [Fact]
    public void ToResult_FromCasResult_WhenIsSuccessFalseButEntityNotNull_ShouldReturnFailure()
    {
        var order = new OrderEntity { Id = "o1", Total = 100 };
        var conflict = ConcurrencyConflict.VersionMismatch("o1", "OrderEntity", ExpectedVersion.Specific(1), ActualVersion.From(2));
        var cas = new CasResult<OrderEntity>(order, null, conflict);

        Result<OrderEntity> result = cas.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToResult_FromEmptyOrNullConflictCasResult_ShouldReturnDefaultConflictError()
    {
        var cas = default(CasResult<OrderEntity>);
        Result<OrderEntity> result = cas.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ConcurrencyErrors.ConcurrencyConflictCode);
        result.Error.Description.Should().Be("Compare-and-swap operation failed without returning a state.");
    }

    [Fact]
    public void ToResult_FromNullableConflict_ShouldHandleNullAndNonNull()
    {
        ConcurrencyConflict? nullConflict = null;
        ResultInstance successResult = nullConflict.ToResult();
        successResult.IsSuccess.Should().BeTrue();

        ConcurrencyConflict? nonNullConflict = ConcurrencyConflict.Deleted("item-1", "Inventory");
        ResultInstance failureResult = nonNullConflict.ToResult();
        failureResult.IsSuccess.Should().BeFalse();
        failureResult.Error.Code.Should().Be(ConcurrencyErrors.EntityDeletedCode);
    }

    [Fact]
    public void ToResult_FromConflictResolution_Resolved_ShouldReturnSuccess()
    {
        var order = new OrderEntity { Id = "o1", Total = 150 };
        ConflictResolution<OrderEntity> resolution = ConflictResolution.Merged(order, "Merged order.");

        Result<OrderEntity> result = resolution.ToResult();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(order);
    }

    [Fact]
    public void ToResult_FromConflictResolution_WhenIsResolvedFalseButEntityNotNull_ShouldReturnFailure()
    {
        var order = new OrderEntity { Id = "o1", Total = 150 };
        var resolution = new ConflictResolution<OrderEntity>(order, ConflictResolutionStrategy.Reject, "Rejected");

        Result<OrderEntity> result = resolution.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToResult_FromConflictResolution_WhenIsResolvedTrueButEntityNull_ShouldReturnFailure()
    {
        var resolution = new ConflictResolution<OrderEntity>(null, ConflictResolutionStrategy.MergeDomainSpecific, "Merged");

        Result<OrderEntity> result = resolution.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ToResult_FromConflictResolution_Rejected_WithReason_ShouldReturnFailureWithReason()
    {
        ConflictResolution<OrderEntity> resolution = ConflictResolution.Rejected<OrderEntity>("Custom reject reason.");

        Result<OrderEntity> result = resolution.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ConcurrencyErrors.ConcurrencyConflictCode);
        result.Error.Description.Should().Be("Custom reject reason.");
    }

    [Fact]
    public void ToResult_FromConflictResolution_Rejected_WithoutReason_ShouldReturnDefaultFailure()
    {
        var resolution = new ConflictResolution<OrderEntity>(null, ConflictResolutionStrategy.Reject, null);

        Result<OrderEntity> result = resolution.ToResult();

        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be(ConcurrencyErrors.ConcurrencyConflictCode);
        result.Error.Description.Should().Be("Conflict rejected by resolution policy.");
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void FromRowsAffected_NonGeneric_ShouldEvaluateCorrectly(int rows, bool expectedSuccess)
    {
        ResultInstance result = ConcurrencyResultExtensions.FromRowsAffected(rows, "cust_1", "Customer", ExpectedVersion.Specific(5));

        result.IsSuccess.Should().Be(expectedSuccess);
        if (!expectedSuccess)
        {
            result.Error.Type.Should().Be(ErrorType.Conflict);
            result.Error.Code.Should().Be(ConcurrencyErrors.VersionMismatchCode);
        }
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void FromRowsAffected_Generic_ShouldEvaluateCorrectly(int rows, bool expectedSuccess)
    {
        var order = new OrderEntity { Id = "o2", Total = 50 };
        Result<OrderEntity> genericResult = ConcurrencyResultExtensions.FromRowsAffected(rows, order, "o2", ExpectedVersion.Specific(3));

        genericResult.IsSuccess.Should().Be(expectedSuccess);
        if (expectedSuccess)
        {
            genericResult.Value.Should().Be(order);
        }
        else
        {
            genericResult.Error.Type.Should().Be(ErrorType.Conflict);
            genericResult.Error.Code.Should().Be(ConcurrencyErrors.VersionMismatchCode);
        }
    }

    [Fact]
    public void FromRowsAffected_Generic_NullEntity_ShouldThrowArgumentNullException()
    {
        Action act = () => ConcurrencyResultExtensions.FromRowsAffected<OrderEntity>(1, null!, "o2", ExpectedVersion.Specific(3));
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }
}
