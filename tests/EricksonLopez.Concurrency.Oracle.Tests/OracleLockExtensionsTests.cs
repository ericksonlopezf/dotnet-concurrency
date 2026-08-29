// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Oracle;
using Xunit;

namespace EricksonLopez.Concurrency.Oracle.Tests;

public sealed class OracleLockExtensionsTests
{
    [Fact]
    public void WithOracleLock_AllValidModes_ShouldAppendCorrectClauses()
    {
        string baseSql = "SELECT * FROM accounts WHERE id = :Id;";

        baseSql.WithOracleLock(OracleLockMode.ForUpdate).Should().Be("SELECT * FROM accounts WHERE id = :Id FOR UPDATE;");
        baseSql.WithOracleLock(OracleLockMode.ForUpdateNowait).Should().Be("SELECT * FROM accounts WHERE id = :Id FOR UPDATE NOWAIT;");
        baseSql.WithOracleLock(OracleLockMode.ForUpdateSkipLocked).Should().Be("SELECT * FROM accounts WHERE id = :Id FOR UPDATE SKIP LOCKED;");
    }

    [Fact]
    public void WithOracleLock_ShouldTrimSemicolonsAndSpaces()
    {
        string sqlWithSpaces = "SELECT * FROM products WHERE id = :Id;   ";
        sqlWithSpaces.WithOracleLock(OracleLockMode.ForUpdate).Should().Be("SELECT * FROM products WHERE id = :Id FOR UPDATE;");

        string sqlWithoutSemicolon = "SELECT * FROM products WHERE id = :Id";
        sqlWithoutSemicolon.WithOracleLock(OracleLockMode.ForUpdateNowait).Should().Be("SELECT * FROM products WHERE id = :Id FOR UPDATE NOWAIT;");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithOracleLock_NullOrWhiteSpace_ShouldThrowArgumentException(string? sql)
    {
        Action act = () => sql!.WithOracleLock(OracleLockMode.ForUpdate);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sqlQuery");
    }

    [Fact]
    public void WithOracleLock_InvalidMode_ShouldThrowArgumentOutOfRangeException()
    {
        var invalidMode = (OracleLockMode)99;
        Action act = () => "SELECT * FROM orders".WithOracleLock(invalidMode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lockMode")
            .WithMessage("*Unsupported Oracle lock mode.*");
    }

    [Fact]
    public void WithOracleLockWait_ValidTimeouts_ShouldAppendCorrectClauses()
    {
        "SELECT * FROM items WHERE id = :Id;".WithOracleLockWait(5)
            .Should().Be("SELECT * FROM items WHERE id = :Id FOR UPDATE WAIT 5;");

        "SELECT * FROM items WHERE id = :Id;   ".WithOracleLockWait(0)
            .Should().Be("SELECT * FROM items WHERE id = :Id FOR UPDATE WAIT 0;");

        "SELECT * FROM items WHERE id = :Id".WithOracleLockWait(10)
            .Should().Be("SELECT * FROM items WHERE id = :Id FOR UPDATE WAIT 10;");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithOracleLockWait_NullOrWhiteSpace_ShouldThrowArgumentException(string? sql)
    {
        Action act = () => sql!.WithOracleLockWait(5);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sqlQuery");
    }

    [Fact]
    public void WithOracleLockWait_NegativeTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        Action act = () => "SELECT * FROM items".WithOracleLockWait(-1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeoutSeconds");
    }
}
