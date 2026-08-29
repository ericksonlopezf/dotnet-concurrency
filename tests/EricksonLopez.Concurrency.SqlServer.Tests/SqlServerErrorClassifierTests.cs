// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.SqlServer;
using Microsoft.Data.SqlClient;
using Xunit;

namespace EricksonLopez.Concurrency.SqlServer.Tests;

public sealed class SqlServerErrorClassifierTests
{
    private static SqlException CreateSqlException(int number, string message = "SQL Server error")
    {
        var collectionConstructor = typeof(SqlErrorCollection).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        var errorCollection = (SqlErrorCollection)collectionConstructor!.Invoke(null);

        var errorCtors = typeof(SqlError).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
        object? error = null;
        foreach (var ctor in errorCtors)
        {
            var pars = ctor.GetParameters();
            var args = new object?[pars.Length];
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].ParameterType == typeof(int) && i == 0) args[i] = number;
                else if (pars[i].ParameterType == typeof(int)) args[i] = number;
                else if (pars[i].ParameterType == typeof(byte)) args[i] = (byte)0;
                else if (pars[i].ParameterType == typeof(string)) args[i] = message;
                else if (pars[i].ParameterType == typeof(uint)) args[i] = 0u;
                else args[i] = null;
            }
            try
            {
                error = ctor.Invoke(args);
                break;
            }
            catch { }
        }

        var addMethod = typeof(SqlErrorCollection).GetMethod(
            "Add",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(SqlError) },
            null);
        addMethod!.Invoke(errorCollection, new[] { error });

        var createMethod = typeof(SqlException).GetMethod(
            "CreateException",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[] { typeof(SqlErrorCollection), typeof(string) },
            null);

        if (createMethod is not null)
        {
            return (SqlException)createMethod.Invoke(null, new object[] { errorCollection, "11.0.0" })!;
        }

        throw new InvalidOperationException("Could not create SqlException via reflection.");
    }

    [Fact]
    public void IsDeadlock_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqlServerErrorClassifier.IsDeadlock(null).Should().BeFalse();
        SqlServerErrorClassifier.IsDeadlock(new InvalidOperationException("Generic")).Should().BeFalse();

        SqlException deadlockEx = CreateSqlException(1205, "Deadlock occurred");
        SqlServerErrorClassifier.IsDeadlock(deadlockEx).Should().BeTrue();

        SqlException otherSqlEx = CreateSqlException(2601, "Unique violation");
        SqlServerErrorClassifier.IsDeadlock(otherSqlEx).Should().BeFalse();

        var wrappedDeadlock = new InvalidOperationException("Wrapped", deadlockEx);
        SqlServerErrorClassifier.IsDeadlock(wrappedDeadlock).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherSqlEx);
        SqlServerErrorClassifier.IsDeadlock(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsSerializationFailure_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqlServerErrorClassifier.IsSerializationFailure(null).Should().BeFalse();
        SqlServerErrorClassifier.IsSerializationFailure(new InvalidOperationException("Generic")).Should().BeFalse();

        SqlException conflict3960 = CreateSqlException(3960, "Snapshot conflict");
        SqlServerErrorClassifier.IsSerializationFailure(conflict3960).Should().BeTrue();

        SqlException conflict3961 = CreateSqlException(3961, "Snapshot update conflict");
        SqlServerErrorClassifier.IsSerializationFailure(conflict3961).Should().BeTrue();

        SqlException otherSqlEx = CreateSqlException(1205, "Deadlock");
        SqlServerErrorClassifier.IsSerializationFailure(otherSqlEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", conflict3960);
        SqlServerErrorClassifier.IsSerializationFailure(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherSqlEx);
        SqlServerErrorClassifier.IsSerializationFailure(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsLockTimeout_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqlServerErrorClassifier.IsLockTimeout(null).Should().BeFalse();
        SqlServerErrorClassifier.IsLockTimeout(new InvalidOperationException("Generic")).Should().BeFalse();

        SqlException lockTimeoutEx = CreateSqlException(1222, "Lock request timeout");
        SqlServerErrorClassifier.IsLockTimeout(lockTimeoutEx).Should().BeTrue();

        SqlException otherSqlEx = CreateSqlException(1205, "Deadlock");
        SqlServerErrorClassifier.IsLockTimeout(otherSqlEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", lockTimeoutEx);
        SqlServerErrorClassifier.IsLockTimeout(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherSqlEx);
        SqlServerErrorClassifier.IsLockTimeout(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        SqlServerErrorClassifier.IsUniqueViolation(null).Should().BeFalse();
        SqlServerErrorClassifier.IsUniqueViolation(new InvalidOperationException("Generic")).Should().BeFalse();

        SqlException unique2601 = CreateSqlException(2601, "Unique index violation");
        SqlServerErrorClassifier.IsUniqueViolation(unique2601).Should().BeTrue();

        SqlException unique2627 = CreateSqlException(2627, "Primary key violation");
        SqlServerErrorClassifier.IsUniqueViolation(unique2627).Should().BeTrue();

        SqlException otherSqlEx = CreateSqlException(1205, "Deadlock");
        SqlServerErrorClassifier.IsUniqueViolation(otherSqlEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", unique2601);
        SqlServerErrorClassifier.IsUniqueViolation(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherSqlEx);
        SqlServerErrorClassifier.IsUniqueViolation(wrappedOther).Should().BeFalse();
    }

    [Theory]
    [InlineData(1205, true)]
    [InlineData(3960, true)]
    [InlineData(3961, true)]
    [InlineData(1222, true)]
    [InlineData(-2, true)]
    [InlineData(4060, true)]
    [InlineData(40197, true)]
    [InlineData(40501, true)]
    [InlineData(40613, true)]
    [InlineData(49918, true)]
    [InlineData(49919, true)]
    [InlineData(49920, true)]
    [InlineData(2601, false)]
    [InlineData(102, false)]
    public void IsTransient_SqlExceptionNumbers_ShouldBeEvaluated(int errorNumber, bool expectedTransient)
    {
        SqlException sqlEx = CreateSqlException(errorNumber, "Test transient");
        SqlServerErrorClassifier.IsTransient(sqlEx).Should().Be(expectedTransient);

        var wrapped = new InvalidOperationException("Outer", sqlEx);
        SqlServerErrorClassifier.IsTransient(wrapped).Should().Be(expectedTransient);
    }

    [Fact]
    public void IsTransient_TimeoutAndGenericExceptions_ShouldBeEvaluated()
    {
        SqlServerErrorClassifier.IsTransient(null).Should().BeFalse();

        var timeoutEx = new TimeoutException("Operation timed out.");
        SqlServerErrorClassifier.IsTransient(timeoutEx).Should().BeTrue();

        var genericEx = new InvalidOperationException("Generic failure");
        SqlServerErrorClassifier.IsTransient(genericEx).Should().BeFalse();
    }

    [Fact]
    public void ToConcurrencyConflict_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => SqlServerErrorClassifier.ToConcurrencyConflict(null!, "id1", "Entity");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void ToConcurrencyConflict_Deadlock_ShouldReturnConfiguredConflict()
    {
        SqlException sqlEx = CreateSqlException(1205, "Deadlock occurred");
        ConcurrencyConflict? conflict = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "order_1", "Order", "CustomOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("order_1");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Deadlock);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomOp");
        conflict.Message.Should().Contain("SQL Server deadlock detected (Error 1205)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("SqlServer");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("1205");

        // Default operation
        ConcurrencyConflict? conflictDefaultOp = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "order_1", "Order");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_SerializationFailure_ShouldReturnConfiguredConflict()
    {
        SqlException sqlEx = CreateSqlException(3960, "Snapshot conflict occurred");
        ConcurrencyConflict? conflict = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "account_1", "Account", "CustomSerializeOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("account_1");
        conflict.EntityType.Should().Be("Account");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomSerializeOp");
        conflict.Message.Should().Contain("SQL Server snapshot isolation conflict (Error 3960)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("SqlServer");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("3960");

        ConcurrencyConflict? conflictDefaultOp = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "account_1", "Account");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UniqueViolation_ShouldReturnConfiguredConflict()
    {
        SqlException sqlEx = CreateSqlException(2601, "Cannot insert duplicate key");
        ConcurrencyConflict? conflict = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "user_1", "User", "CustomUniqueOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("user_1");
        conflict.EntityType.Should().Be("User");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Custom);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.Operation.Should().Be("CustomUniqueOp");
        conflict.Message.Should().Contain("SQL Server unique constraint violation (Error 2601)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("SqlServer");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("2601");

        ConcurrencyConflict? conflictDefaultOp = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "user_1", "User");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UnrecognizedSqlException_ShouldReturnNull()
    {
        SqlException sqlEx = CreateSqlException(102, "Incorrect syntax near 'FROM'");
        ConcurrencyConflict? conflict = SqlServerErrorClassifier.ToConcurrencyConflict(sqlEx, "user_1", "User");

        conflict.Should().BeNull();
    }
}
