// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Sqlite;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EricksonLopez.Concurrency.Sqlite.Tests;

public sealed class SqliteConcurrencyErrorClassifierTests
{
    [Fact]
    public void IsBusy_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqliteConcurrencyErrorClassifier.IsBusy(null).Should().BeFalse();
        SqliteConcurrencyErrorClassifier.IsBusy(new InvalidOperationException("Generic")).Should().BeFalse();

        var busyEx = new SqliteException("database is locked", 5);
        SqliteConcurrencyErrorClassifier.IsBusy(busyEx).Should().BeTrue();

        var otherEx = new SqliteException("constraint failed", 19);
        SqliteConcurrencyErrorClassifier.IsBusy(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", busyEx);
        SqliteConcurrencyErrorClassifier.IsBusy(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        SqliteConcurrencyErrorClassifier.IsBusy(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsLocked_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqliteConcurrencyErrorClassifier.IsLocked(null).Should().BeFalse();
        SqliteConcurrencyErrorClassifier.IsLocked(new InvalidOperationException("Generic")).Should().BeFalse();

        var lockedEx = new SqliteException("table is locked", 6);
        SqliteConcurrencyErrorClassifier.IsLocked(lockedEx).Should().BeTrue();

        var otherEx = new SqliteException("database is locked", 5);
        SqliteConcurrencyErrorClassifier.IsLocked(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", lockedEx);
        SqliteConcurrencyErrorClassifier.IsLocked(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        SqliteConcurrencyErrorClassifier.IsLocked(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsConstraintViolation_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqliteConcurrencyErrorClassifier.IsConstraintViolation(null).Should().BeFalse();
        SqliteConcurrencyErrorClassifier.IsConstraintViolation(new InvalidOperationException("Generic")).Should().BeFalse();

        var constraintEx = new SqliteException("UNIQUE constraint failed: table.column", 19);
        SqliteConcurrencyErrorClassifier.IsConstraintViolation(constraintEx).Should().BeTrue();

        var otherEx = new SqliteException("database is locked", 5);
        SqliteConcurrencyErrorClassifier.IsConstraintViolation(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", constraintEx);
        SqliteConcurrencyErrorClassifier.IsConstraintViolation(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        SqliteConcurrencyErrorClassifier.IsConstraintViolation(wrappedOther).Should().BeFalse();
    }

    [Theory]
    [InlineData(5, true)]
    [InlineData(6, true)]
    [InlineData(19, false)]
    [InlineData(1, false)]
    public void IsTransient_SqliteErrorCode_ShouldBeEvaluated(int errorCode, bool expectedTransient)
    {
        var ex = new SqliteException("Test", errorCode);
        SqliteConcurrencyErrorClassifier.IsTransient(ex).Should().Be(expectedTransient);

        var wrapped = new InvalidOperationException("Outer", ex);
        SqliteConcurrencyErrorClassifier.IsTransient(wrapped).Should().Be(expectedTransient);
    }

    [Fact]
    public void IsTransient_TimeoutAndGenericExceptions_ShouldBeEvaluated()
    {
        SqliteConcurrencyErrorClassifier.IsTransient(null).Should().BeFalse();

        var timeoutEx = new TimeoutException("Operation timed out.");
        SqliteConcurrencyErrorClassifier.IsTransient(timeoutEx).Should().BeTrue();

        var wrappedTimeout = new InvalidOperationException("Outer wrapper", timeoutEx);
        SqliteConcurrencyErrorClassifier.IsTransient(wrappedTimeout).Should().BeTrue();

        var genericEx = new InvalidOperationException("Generic failure");
        SqliteConcurrencyErrorClassifier.IsTransient(genericEx).Should().BeFalse();
    }

    [Fact]
    public void ToConcurrencyConflict_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(null!, "id1", "Entity");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void ToConcurrencyConflict_Busy_ShouldReturnConfiguredConflict()
    {
        var ex = new SqliteException("database is locked", 5);
        ConcurrencyConflict? conflict = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "order_1", "Order", "CustomBusyOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("order_1");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomBusyOp");
        conflict.Message.Should().Contain("SQLite database busy lock conflict (Error 5)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Sqlite");
        conflict.Metadata.Should().ContainKey("errorCode").WhoseValue.Should().Be("5");

        ConcurrencyConflict? conflictDefaultOp = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "order_1", "Order");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_Locked_ShouldReturnConfiguredConflict()
    {
        var ex = new SqliteException("table is locked", 6);
        ConcurrencyConflict? conflict = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account", "CustomLockedOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("account_1");
        conflict.EntityType.Should().Be("Account");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomLockedOp");
        conflict.Message.Should().Contain("SQLite table locked conflict (Error 6)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Sqlite");
        conflict.Metadata.Should().ContainKey("errorCode").WhoseValue.Should().Be("6");

        ConcurrencyConflict? conflictDefaultOp = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_Constraint_ShouldReturnConfiguredConflict()
    {
        var ex = new SqliteException("UNIQUE constraint failed", 19);
        ConcurrencyConflict? conflict = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User", "CustomConstraintOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("user_1");
        conflict.EntityType.Should().Be("User");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Custom);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.Operation.Should().Be("CustomConstraintOp");
        conflict.Message.Should().Contain("SQLite constraint violation (Error 19)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Sqlite");
        conflict.Metadata.Should().ContainKey("errorCode").WhoseValue.Should().Be("19");

        ConcurrencyConflict? conflictDefaultOp = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UnrecognizedSqliteException_ShouldReturnNull()
    {
        var ex = new SqliteException("syntax error", 1);
        ConcurrencyConflict? conflict = SqliteConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User");

        conflict.Should().BeNull();
    }
}
