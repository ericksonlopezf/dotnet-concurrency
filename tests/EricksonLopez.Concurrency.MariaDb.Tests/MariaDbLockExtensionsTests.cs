// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.MariaDb;
using Xunit;

namespace EricksonLopez.Concurrency.MariaDb.Tests;

public sealed class MariaDbLockExtensionsTests
{
    [Fact]
    public void WithMariaDbLock_AllValidModes_ShouldAppendCorrectClauses()
    {
        string baseSql = "SELECT * FROM accounts WHERE id = @Id;";

        baseSql.WithMariaDbLock(MariaDbLockMode.ForUpdate).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE;");
        baseSql.WithMariaDbLock(MariaDbLockMode.ForUpdateNowait).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE NOWAIT;");
        baseSql.WithMariaDbLock(MariaDbLockMode.ForUpdateSkipLocked).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE SKIP LOCKED;");
        baseSql.WithMariaDbLock(MariaDbLockMode.LockInShareMode).Should().Be("SELECT * FROM accounts WHERE id = @Id LOCK IN SHARE MODE;");
    }

    [Fact]
    public void WithMariaDbLock_ShouldTrimSemicolonsAndSpaces()
    {
        string sqlWithSpaces = "SELECT * FROM products WHERE id = @Id;   ";
        sqlWithSpaces.WithMariaDbLock(MariaDbLockMode.ForUpdate).Should().Be("SELECT * FROM products WHERE id = @Id FOR UPDATE;");

        string sqlWithoutSemicolon = "SELECT * FROM products WHERE id = @Id";
        sqlWithoutSemicolon.WithMariaDbLock(MariaDbLockMode.LockInShareMode).Should().Be("SELECT * FROM products WHERE id = @Id LOCK IN SHARE MODE;");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithMariaDbLock_NullOrWhiteSpace_ShouldThrowArgumentException(string? sql)
    {
        Action act = () => sql!.WithMariaDbLock(MariaDbLockMode.ForUpdate);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sqlQuery");
    }

    [Fact]
    public void WithMariaDbLock_InvalidMode_ShouldThrowArgumentOutOfRangeException()
    {
        var invalidMode = (MariaDbLockMode)99;
        Action act = () => "SELECT * FROM orders".WithMariaDbLock(invalidMode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lockMode")
            .WithMessage("*Unsupported MariaDB lock mode.*");
    }

    [Fact]
    public void WithMariaDbLockWait_ValidTimeouts_ShouldAppendCorrectClauses()
    {
        "SELECT * FROM items WHERE id = @Id;".WithMariaDbLockWait(5)
            .Should().Be("SELECT * FROM items WHERE id = @Id FOR UPDATE WAIT 5;");

        "SELECT * FROM items WHERE id = @Id;   ".WithMariaDbLockWait(0)
            .Should().Be("SELECT * FROM items WHERE id = @Id FOR UPDATE WAIT 0;");

        "SELECT * FROM items WHERE id = @Id".WithMariaDbLockWait(10)
            .Should().Be("SELECT * FROM items WHERE id = @Id FOR UPDATE WAIT 10;");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithMariaDbLockWait_NullOrWhiteSpace_ShouldThrowArgumentException(string? sql)
    {
        Action act = () => sql!.WithMariaDbLockWait(5);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sqlQuery");
    }

    [Fact]
    public void WithMariaDbLockWait_NegativeTimeout_ShouldThrowArgumentOutOfRangeException()
    {
        Action act = () => "SELECT * FROM items".WithMariaDbLockWait(-1);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("timeoutSeconds");
    }
}
