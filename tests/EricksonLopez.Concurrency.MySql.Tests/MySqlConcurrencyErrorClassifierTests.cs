// Copyright © Erickson Lopez. MIT License.
using System;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.MySql;
using MySqlConnector;
using Xunit;

namespace EricksonLopez.Concurrency.MySql.Tests;

public sealed class MySqlConcurrencyErrorClassifierTests
{
    private static MySqlException CreateMySqlException(int number, string message = "MySQL error")
    {
        var ctors = typeof(MySqlException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        foreach (var ctor in ctors)
        {
            var pars = ctor.GetParameters();
            var args = new object?[pars.Length];
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].ParameterType == typeof(MySqlErrorCode)) args[i] = (MySqlErrorCode)number;
                else if (pars[i].ParameterType == typeof(int)) args[i] = number;
                else if (pars[i].ParameterType == typeof(string) && pars[i].Name?.Contains("sqlState", StringComparison.OrdinalIgnoreCase) == true) args[i] = "HY000";
                else if (pars[i].ParameterType == typeof(string)) args[i] = message;
                else if (pars[i].ParameterType == typeof(Exception)) args[i] = null;
                else args[i] = null;
            }
            try
            {
                var instance = (MySqlException)ctor.Invoke(args);
                if (instance.Number == number) return instance;
            }
            catch { }
        }

        var methods = typeof(MySqlException).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        foreach (var method in methods)
        {
            if (method.ReturnType == typeof(MySqlException))
            {
                var pars = method.GetParameters();
                var args = new object?[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    if (pars[i].ParameterType == typeof(MySqlErrorCode)) args[i] = (MySqlErrorCode)number;
                    else if (pars[i].ParameterType == typeof(int)) args[i] = number;
                    else if (pars[i].ParameterType == typeof(string)) args[i] = message;
                    else args[i] = null;
                }
                try
                {
                    var instance = (MySqlException)method.Invoke(null, args)!;
                    if (instance.Number == number) return instance;
                }
                catch { }
            }
        }

        throw new InvalidOperationException("Could not create MySqlException via reflection.");
    }

    [Fact]
    public void IsDeadlock_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        MySqlConcurrencyErrorClassifier.IsDeadlock(null).Should().BeFalse();
        MySqlConcurrencyErrorClassifier.IsDeadlock(new InvalidOperationException("Generic")).Should().BeFalse();

        MySqlException deadlockEx = CreateMySqlException(1213, "Deadlock occurred");
        MySqlConcurrencyErrorClassifier.IsDeadlock(deadlockEx).Should().BeTrue();

        MySqlException otherEx = CreateMySqlException(1062, "Duplicate key");
        MySqlConcurrencyErrorClassifier.IsDeadlock(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", deadlockEx);
        MySqlConcurrencyErrorClassifier.IsDeadlock(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        MySqlConcurrencyErrorClassifier.IsDeadlock(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsLockTimeout_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        MySqlConcurrencyErrorClassifier.IsLockTimeout(null).Should().BeFalse();
        MySqlConcurrencyErrorClassifier.IsLockTimeout(new InvalidOperationException("Generic")).Should().BeFalse();

        MySqlException timeoutEx = CreateMySqlException(1205, "Lock wait timeout");
        MySqlConcurrencyErrorClassifier.IsLockTimeout(timeoutEx).Should().BeTrue();

        MySqlException otherEx = CreateMySqlException(1213, "Deadlock");
        MySqlConcurrencyErrorClassifier.IsLockTimeout(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", timeoutEx);
        MySqlConcurrencyErrorClassifier.IsLockTimeout(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        MySqlConcurrencyErrorClassifier.IsLockTimeout(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        MySqlConcurrencyErrorClassifier.IsUniqueViolation(null).Should().BeFalse();
        MySqlConcurrencyErrorClassifier.IsUniqueViolation(new InvalidOperationException("Generic")).Should().BeFalse();

        MySqlException duplicateEx = CreateMySqlException(1062, "Duplicate entry");
        MySqlConcurrencyErrorClassifier.IsUniqueViolation(duplicateEx).Should().BeTrue();

        MySqlException otherEx = CreateMySqlException(1213, "Deadlock");
        MySqlConcurrencyErrorClassifier.IsUniqueViolation(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", duplicateEx);
        MySqlConcurrencyErrorClassifier.IsUniqueViolation(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        MySqlConcurrencyErrorClassifier.IsUniqueViolation(wrappedOther).Should().BeFalse();
    }

    [Theory]
    [InlineData(1213, true)]
    [InlineData(1205, true)]
    [InlineData(1042, true)]
    [InlineData(1053, true)]
    [InlineData(1158, true)]
    [InlineData(1159, true)]
    [InlineData(1160, true)]
    [InlineData(1161, true)]
    [InlineData(1062, false)]
    [InlineData(1064, false)]
    public void IsTransient_MySqlExceptionNumbers_ShouldBeEvaluated(int errorNumber, bool expectedTransient)
    {
        MySqlException mySqlEx = CreateMySqlException(errorNumber, "Test transient");
        MySqlConcurrencyErrorClassifier.IsTransient(mySqlEx).Should().Be(expectedTransient);

        var wrapped = new InvalidOperationException("Outer", mySqlEx);
        MySqlConcurrencyErrorClassifier.IsTransient(wrapped).Should().Be(expectedTransient);
    }

    [Fact]
    public void IsTransient_TimeoutAndGenericExceptions_ShouldBeEvaluated()
    {
        MySqlConcurrencyErrorClassifier.IsTransient(null).Should().BeFalse();

        var timeoutEx = new TimeoutException("Operation timed out.");
        MySqlConcurrencyErrorClassifier.IsTransient(timeoutEx).Should().BeTrue();

        var genericEx = new InvalidOperationException("Generic failure");
        MySqlConcurrencyErrorClassifier.IsTransient(genericEx).Should().BeFalse();
    }

    [Fact]
    public void ToConcurrencyConflict_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(null!, "id1", "Entity");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void ToConcurrencyConflict_Deadlock_ShouldReturnConfiguredConflict()
    {
        MySqlException ex = CreateMySqlException(1213, "Deadlock found when trying to get lock");
        ConcurrencyConflict? conflict = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "order_1", "Order", "CustomOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("order_1");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomOp");
        conflict.Message.Should().Contain("MySQL deadlock detected (Error 1213)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("MySql");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("1213");

        ConcurrencyConflict? conflictDefaultOp = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "order_1", "Order");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_LockTimeout_ShouldReturnConfiguredConflict()
    {
        MySqlException ex = CreateMySqlException(1205, "Lock wait timeout exceeded");
        ConcurrencyConflict? conflict = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account", "CustomLockOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("account_1");
        conflict.EntityType.Should().Be("Account");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomLockOp");
        conflict.Message.Should().Contain("MySQL lock wait timeout exceeded (Error 1205)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("MySql");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("1205");

        ConcurrencyConflict? conflictDefaultOp = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UniqueViolation_ShouldReturnConfiguredConflict()
    {
        MySqlException ex = CreateMySqlException(1062, "Duplicate entry 'abc' for key 'PRIMARY'");
        ConcurrencyConflict? conflict = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User", "CustomUniqueOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("user_1");
        conflict.EntityType.Should().Be("User");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Custom);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.Operation.Should().Be("CustomUniqueOp");
        conflict.Message.Should().Contain("MySQL duplicate entry constraint violation (Error 1062)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("MySql");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("1062");

        ConcurrencyConflict? conflictDefaultOp = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UnrecognizedMySqlException_ShouldReturnNull()
    {
        MySqlException ex = CreateMySqlException(1064, "You have an error in your SQL syntax");
        ConcurrencyConflict? conflict = MySqlConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User");

        conflict.Should().BeNull();
    }
}
