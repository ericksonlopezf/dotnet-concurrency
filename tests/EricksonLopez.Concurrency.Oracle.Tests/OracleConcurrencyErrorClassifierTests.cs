// Copyright © Erickson Lopez. MIT License.
using System;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Oracle;
using Oracle.ManagedDataAccess.Client;
using Xunit;

namespace EricksonLopez.Concurrency.Oracle.Tests;

public sealed class OracleConcurrencyErrorClassifierTests
{
    private static OracleException CreateOracleException(int number, string message = "Oracle error")
    {
        var ctors = typeof(OracleException).GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        foreach (var ctor in ctors)
        {
            var pars = ctor.GetParameters();
            var args = new object?[pars.Length];
            for (int i = 0; i < pars.Length; i++)
            {
                if (pars[i].ParameterType == typeof(int)) args[i] = number;
                else if (pars[i].ParameterType == typeof(string)) args[i] = message;
                else if (pars[i].ParameterType == typeof(Exception)) args[i] = null;
                else args[i] = null;
            }
            try
            {
                var instance = (OracleException)ctor.Invoke(args);
                if (instance.Number == number) return instance;
            }
            catch { }
        }

        var methods = typeof(OracleException).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
        foreach (var method in methods)
        {
            if (method.ReturnType == typeof(OracleException))
            {
                var pars = method.GetParameters();
                var args = new object?[pars.Length];
                for (int i = 0; i < pars.Length; i++)
                {
                    if (pars[i].ParameterType == typeof(int)) args[i] = number;
                    else if (pars[i].ParameterType == typeof(string)) args[i] = message;
                    else args[i] = null;
                }
                try
                {
                    var instance = (OracleException)method.Invoke(null, args)!;
                    if (instance.Number == number) return instance;
                }
                catch { }
            }
        }

        throw new InvalidOperationException("Could not create OracleException via reflection.");
    }

    [Fact]
    public void IsDeadlock_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        OracleConcurrencyErrorClassifier.IsDeadlock(null).Should().BeFalse();
        OracleConcurrencyErrorClassifier.IsDeadlock(new InvalidOperationException("Generic")).Should().BeFalse();

        OracleException deadlockEx = CreateOracleException(60, "ORA-00060: deadlock detected");
        OracleConcurrencyErrorClassifier.IsDeadlock(deadlockEx).Should().BeTrue();

        OracleException otherEx = CreateOracleException(1, "ORA-00001: unique constraint violated");
        OracleConcurrencyErrorClassifier.IsDeadlock(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", deadlockEx);
        OracleConcurrencyErrorClassifier.IsDeadlock(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        OracleConcurrencyErrorClassifier.IsDeadlock(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsResourceBusy_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        OracleConcurrencyErrorClassifier.IsResourceBusy(null).Should().BeFalse();
        OracleConcurrencyErrorClassifier.IsResourceBusy(new InvalidOperationException("Generic")).Should().BeFalse();

        OracleException busyEx = CreateOracleException(54, "ORA-00054: resource busy and acquire with NOWAIT specified");
        OracleConcurrencyErrorClassifier.IsResourceBusy(busyEx).Should().BeTrue();

        OracleException otherEx = CreateOracleException(60, "ORA-00060");
        OracleConcurrencyErrorClassifier.IsResourceBusy(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", busyEx);
        OracleConcurrencyErrorClassifier.IsResourceBusy(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        OracleConcurrencyErrorClassifier.IsResourceBusy(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsSerializationFailure_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        OracleConcurrencyErrorClassifier.IsSerializationFailure(null).Should().BeFalse();
        OracleConcurrencyErrorClassifier.IsSerializationFailure(new InvalidOperationException("Generic")).Should().BeFalse();

        OracleException serializeEx = CreateOracleException(8177, "ORA-08177: can't serialize access for this transaction");
        OracleConcurrencyErrorClassifier.IsSerializationFailure(serializeEx).Should().BeTrue();

        OracleException otherEx = CreateOracleException(60, "ORA-00060");
        OracleConcurrencyErrorClassifier.IsSerializationFailure(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", serializeEx);
        OracleConcurrencyErrorClassifier.IsSerializationFailure(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        OracleConcurrencyErrorClassifier.IsSerializationFailure(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        OracleConcurrencyErrorClassifier.IsUniqueViolation(null).Should().BeFalse();
        OracleConcurrencyErrorClassifier.IsUniqueViolation(new InvalidOperationException("Generic")).Should().BeFalse();

        OracleException uniqueEx = CreateOracleException(1, "ORA-00001: unique constraint violated");
        OracleConcurrencyErrorClassifier.IsUniqueViolation(uniqueEx).Should().BeTrue();

        OracleException otherEx = CreateOracleException(60, "ORA-00060");
        OracleConcurrencyErrorClassifier.IsUniqueViolation(otherEx).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", uniqueEx);
        OracleConcurrencyErrorClassifier.IsUniqueViolation(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", otherEx);
        OracleConcurrencyErrorClassifier.IsUniqueViolation(wrappedOther).Should().BeFalse();
    }

    [Theory]
    [InlineData(60, true)]
    [InlineData(54, true)]
    [InlineData(8177, true)]
    [InlineData(3113, true)]
    [InlineData(3114, true)]
    [InlineData(12170, true)]
    [InlineData(12541, true)]
    [InlineData(12543, true)]
    [InlineData(1, false)]
    [InlineData(942, false)]
    public void IsTransient_OracleExceptionNumbers_ShouldBeEvaluated(int errorNumber, bool expectedTransient)
    {
        OracleException oraEx = CreateOracleException(errorNumber, "Test transient");
        OracleConcurrencyErrorClassifier.IsTransient(oraEx).Should().Be(expectedTransient);

        var wrapped = new InvalidOperationException("Outer", oraEx);
        OracleConcurrencyErrorClassifier.IsTransient(wrapped).Should().Be(expectedTransient);
    }

    [Fact]
    public void IsTransient_TimeoutAndGenericExceptions_ShouldBeEvaluated()
    {
        OracleConcurrencyErrorClassifier.IsTransient(null).Should().BeFalse();

        var timeoutEx = new TimeoutException("Operation timed out.");
        OracleConcurrencyErrorClassifier.IsTransient(timeoutEx).Should().BeTrue();

        var genericEx = new InvalidOperationException("Generic failure");
        OracleConcurrencyErrorClassifier.IsTransient(genericEx).Should().BeFalse();
    }

    [Fact]
    public void ToConcurrencyConflict_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => OracleConcurrencyErrorClassifier.ToConcurrencyConflict(null!, "id1", "Entity");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void ToConcurrencyConflict_Deadlock_ShouldReturnConfiguredConflict()
    {
        OracleException ex = CreateOracleException(60, "deadlock detected");
        ConcurrencyConflict? conflict = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "order_1", "Order", "CustomDeadlockOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("order_1");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomDeadlockOp");
        conflict.Message.Should().Contain("Oracle deadlock detected (ORA-00060)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Oracle");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("60");

        ConcurrencyConflict? conflictDefaultOp = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "order_1", "Order");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_ResourceBusy_ShouldReturnConfiguredConflict()
    {
        OracleException ex = CreateOracleException(54, "resource busy");
        ConcurrencyConflict? conflict = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account", "CustomBusyOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("account_1");
        conflict.EntityType.Should().Be("Account");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomBusyOp");
        conflict.Message.Should().Contain("Oracle resource busy condition (ORA-00054)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Oracle");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("54");

        ConcurrencyConflict? conflictDefaultOp = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_SerializationFailure_ShouldReturnConfiguredConflict()
    {
        OracleException ex = CreateOracleException(8177, "can't serialize access");
        ConcurrencyConflict? conflict = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account", "CustomSerializeOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("account_1");
        conflict.EntityType.Should().Be("Account");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomSerializeOp");
        conflict.Message.Should().Contain("Oracle serialization conflict (ORA-08177)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Oracle");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("8177");

        ConcurrencyConflict? conflictDefaultOp = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "account_1", "Account");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UniqueViolation_ShouldReturnConfiguredConflict()
    {
        OracleException ex = CreateOracleException(1, "unique constraint violated");
        ConcurrencyConflict? conflict = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User", "CustomUniqueOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("user_1");
        conflict.EntityType.Should().Be("User");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Custom);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.StaleState);
        conflict.Operation.Should().Be("CustomUniqueOp");
        conflict.Message.Should().Contain("Oracle unique constraint violation (ORA-00001)");
        conflict.Metadata.Should().ContainKey("provider").WhoseValue.Should().Be("Oracle");
        conflict.Metadata.Should().ContainKey("errorNumber").WhoseValue.Should().Be("1");

        ConcurrencyConflict? conflictDefaultOp = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User");
        conflictDefaultOp!.Operation.Should().Be("Update");
    }

    [Fact]
    public void ToConcurrencyConflict_UnrecognizedOracleException_ShouldReturnNull()
    {
        OracleException ex = CreateOracleException(942, "table or view does not exist");
        ConcurrencyConflict? conflict = OracleConcurrencyErrorClassifier.ToConcurrencyConflict(ex, "user_1", "User");

        conflict.Should().BeNull();
    }
}
