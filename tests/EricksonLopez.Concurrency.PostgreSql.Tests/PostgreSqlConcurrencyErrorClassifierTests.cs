// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.PostgreSql;
using Npgsql;
using Xunit;

namespace EricksonLopez.Concurrency.PostgreSql.Tests;

public sealed class PostgreSqlConcurrencyErrorClassifierTests
{
    private sealed class TransientNpgsqlException : NpgsqlException
    {
        public TransientNpgsqlException(string message) : base(message) { }
        public override bool IsTransient => true;
    }

    private sealed class NonTransientNpgsqlException : NpgsqlException
    {
        public NonTransientNpgsqlException(string message) : base(message) { }
        public override bool IsTransient => false;
    }

    [Fact]
    public void Constants_ShouldHaveExpectedValues()
    {
        PostgreSqlConcurrencyErrorClassifier.SerializationFailureSqlState.Should().Be("40001");
        PostgreSqlConcurrencyErrorClassifier.DeadlockDetectedSqlState.Should().Be("40P01");
        PostgreSqlConcurrencyErrorClassifier.LockNotAvailableSqlState.Should().Be("55P03");
        PostgreSqlConcurrencyErrorClassifier.UniqueViolationSqlState.Should().Be("23505");
        PostgreSqlConcurrencyErrorClassifier.InFailedSqlTransactionSqlState.Should().Be("25P02");
    }

    [Fact]
    public void IsSerializationFailure_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(null).Should().BeFalse();
        PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(new InvalidOperationException("Generic error")).Should().BeFalse();

        var pgDirect = new PostgresException("Serialization failure", "ERROR", "ERROR", "40001");
        PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(pgDirect).Should().BeTrue();

        var pgOther = new PostgresException("Other", "ERROR", "ERROR", "23505");
        PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(pgOther).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", pgDirect);
        PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", pgOther);
        PostgreSqlConcurrencyErrorClassifier.IsSerializationFailure(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsDeadlock_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        PostgreSqlConcurrencyErrorClassifier.IsDeadlock(null).Should().BeFalse();
        PostgreSqlConcurrencyErrorClassifier.IsDeadlock(new InvalidOperationException("Generic error")).Should().BeFalse();

        var pgDirect = new PostgresException("Deadlock detected", "ERROR", "ERROR", "40P01");
        PostgreSqlConcurrencyErrorClassifier.IsDeadlock(pgDirect).Should().BeTrue();

        var pgOther = new PostgresException("Other", "ERROR", "ERROR", "40001");
        PostgreSqlConcurrencyErrorClassifier.IsDeadlock(pgOther).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", pgDirect);
        PostgreSqlConcurrencyErrorClassifier.IsDeadlock(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", pgOther);
        PostgreSqlConcurrencyErrorClassifier.IsDeadlock(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsLockNotAvailable_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(null).Should().BeFalse();
        PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(new InvalidOperationException("Generic error")).Should().BeFalse();

        var pgDirect = new PostgresException("Lock unavailable", "ERROR", "ERROR", "55P03");
        PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(pgDirect).Should().BeTrue();

        var pgOther = new PostgresException("Other", "ERROR", "ERROR", "40P01");
        PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(pgOther).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", pgDirect);
        PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", pgOther);
        PostgreSqlConcurrencyErrorClassifier.IsLockNotAvailable(wrappedOther).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueViolation_DirectAndNestedExceptions_ShouldBeEvaluated()
    {
        PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(null).Should().BeFalse();
        PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(new InvalidOperationException("Generic error")).Should().BeFalse();

        var pgDirect = new PostgresException("Unique violation", "ERROR", "ERROR", "23505");
        PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(pgDirect).Should().BeTrue();

        var pgOther = new PostgresException("Other", "ERROR", "ERROR", "40001");
        PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(pgOther).Should().BeFalse();

        var wrapped = new InvalidOperationException("Wrapped", pgDirect);
        PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(wrapped).Should().BeTrue();

        var wrappedOther = new InvalidOperationException("Wrapped", pgOther);
        PostgreSqlConcurrencyErrorClassifier.IsUniqueViolation(wrappedOther).Should().BeFalse();
    }

    [Theory]
    [InlineData("40001", true)]
    [InlineData("40P01", true)]
    [InlineData("08006", true)]
    [InlineData("57P01", true)]
    [InlineData("57P02", true)]
    [InlineData("57P03", true)]
    [InlineData("23505", false)]
    [InlineData("55P03", false)]
    [InlineData("42P01", false)]
    [InlineData("", false)]
    public void IsTransient_PostgresExceptionSqlStates_ShouldBeClassified(string sqlState, bool expectedTransient)
    {
        var pgEx = new PostgresException("Message", "ERROR", "ERROR", sqlState);
        PostgreSqlConcurrencyErrorClassifier.IsTransient(pgEx).Should().Be(expectedTransient);

        var wrapped = new InvalidOperationException("Outer", pgEx);
        PostgreSqlConcurrencyErrorClassifier.IsTransient(wrapped).Should().Be(expectedTransient);
    }

    [Fact]
    public void IsTransient_NpgsqlExceptionAndNullAndOther_ShouldBeClassified()
    {
        PostgreSqlConcurrencyErrorClassifier.IsTransient(null).Should().BeFalse();

        var transientNpgEx = new TransientNpgsqlException("Transient Npgsql");
        PostgreSqlConcurrencyErrorClassifier.IsTransient(transientNpgEx).Should().BeTrue();

        var nonTransientNpgEx = new NonTransientNpgsqlException("Non-transient Npgsql");
        PostgreSqlConcurrencyErrorClassifier.IsTransient(nonTransientNpgEx).Should().BeFalse();

        var genericEx = new InvalidOperationException("Not transient");
        PostgreSqlConcurrencyErrorClassifier.IsTransient(genericEx).Should().BeFalse();
    }

    [Fact]
    public void ToConcurrencyConflict_Null_ShouldThrowArgumentNullException()
    {
        Action act = () => PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(null!, "id1", "Entity");
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exception");
    }

    [Fact]
    public void ToConcurrencyConflict_UnrecognizedException_ShouldReturnNull()
    {
        PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(new InvalidOperationException(), "id1", "Entity").Should().BeNull();
    }

    [Fact]
    public void ToConcurrencyConflict_SerializationFailure_ShouldReturnConfiguredConflict()
    {
        var pgEx = new PostgresException("could not serialize access", "ERROR", "ERROR", "40001");
        ConcurrencyConflict? conflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(pgEx, "acc_1", "Account", "CustomOp");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("acc_1");
        conflict.EntityType.Should().Be("Account");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.SerializationFailure);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("CustomOp");
        conflict.Message.Should().Be("PostgreSQL transaction serialization conflict (SQLSTATE 40001) on 'Account' with ID 'acc_1'.");
        conflict.Metadata.Should().ContainKey("sqlState").WhoseValue.Should().Be("40001");
    }

    [Fact]
    public void ToConcurrencyConflict_Deadlock_ShouldReturnConfiguredConflict()
    {
        var pgEx = new PostgresException("deadlock detected", "ERROR", "ERROR", "40P01");
        ConcurrencyConflict? conflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(pgEx, "ord_9", "Order");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("ord_9");
        conflict.EntityType.Should().Be("Order");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.Deadlock);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.Transient);
        conflict.Operation.Should().Be("PostgreSqlOperation");
        conflict.Message.Should().Be("PostgreSQL deadlock detected (SQLSTATE 40P01) on 'Order' with ID 'ord_9'.");
        conflict.Metadata.Should().ContainKey("sqlState").WhoseValue.Should().Be("40P01");
    }

    [Fact]
    public void ToConcurrencyConflict_LockNotAvailable_ShouldReturnConfiguredConflict()
    {
        var pgEx = new PostgresException("could not obtain lock", "ERROR", "ERROR", "55P03");
        ConcurrencyConflict? conflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(pgEx, "res_5", "Resource");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("res_5");
        conflict.EntityType.Should().Be("Resource");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.LockUnavailable);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.NonRetryable);
        conflict.Operation.Should().Be("PostgreSqlOperation");
        conflict.Message.Should().Be("PostgreSQL row lock unavailable (SQLSTATE 55P03) on 'Resource' with ID 'res_5'.");
        conflict.Metadata.Should().ContainKey("sqlState").WhoseValue.Should().Be("55P03");
    }

    [Fact]
    public void ToConcurrencyConflict_UniqueViolation_ShouldReturnConfiguredConflict()
    {
        var pgEx = new PostgresException("duplicate key value", "ERROR", "ERROR", "23505");
        ConcurrencyConflict? conflict = PostgreSqlConcurrencyErrorClassifier.ToConcurrencyConflict(pgEx, "usr_10", "User");

        conflict.Should().NotBeNull();
        conflict!.EntityId.Should().Be("usr_10");
        conflict.EntityType.Should().Be("User");
        conflict.ConflictType.Should().Be(ConcurrencyConflictType.AlreadyExists);
        conflict.Classification.Should().Be(ConcurrencyConflictClassification.NonRetryable);
        conflict.Operation.Should().Be("PostgreSqlOperation");
        conflict.Message.Should().Be("PostgreSQL unique constraint violation (SQLSTATE 23505) on 'User' with ID 'usr_10'.");
        conflict.Metadata.Should().ContainKey("sqlState").WhoseValue.Should().Be("23505");
    }
}
