// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.MySql;
using Xunit;

namespace EricksonLopez.Concurrency.MySql.Tests;

public sealed class MySqlLockExtensionsTests
{
    [Fact]
    public void WithMySqlLock_AllValidModes_ShouldAppendCorrectClauses()
    {
        string baseSql = "SELECT * FROM accounts WHERE id = @Id;";

        baseSql.WithMySqlLock(MySqlLockMode.ForUpdate).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE;");
        baseSql.WithMySqlLock(MySqlLockMode.ForUpdateNowait).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE NOWAIT;");
        baseSql.WithMySqlLock(MySqlLockMode.ForUpdateSkipLocked).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE SKIP LOCKED;");
        baseSql.WithMySqlLock(MySqlLockMode.ForShare).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR SHARE;");
    }

    [Fact]
    public void WithMySqlLock_ShouldTrimSemicolonsAndSpaces()
    {
        string sqlWithSpaces = "SELECT * FROM products WHERE id = @Id;   ";
        sqlWithSpaces.WithMySqlLock(MySqlLockMode.ForUpdate).Should().Be("SELECT * FROM products WHERE id = @Id FOR UPDATE;");

        string sqlWithoutSemicolon = "SELECT * FROM products WHERE id = @Id";
        sqlWithoutSemicolon.WithMySqlLock(MySqlLockMode.ForShare).Should().Be("SELECT * FROM products WHERE id = @Id FOR SHARE;");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithMySqlLock_NullOrWhiteSpace_ShouldThrowArgumentException(string? sql)
    {
        Action act = () => sql!.WithMySqlLock(MySqlLockMode.ForUpdate);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("sqlQuery");
    }

    [Fact]
    public void WithMySqlLock_InvalidMode_ShouldThrowArgumentOutOfRangeException()
    {
        var invalidMode = (MySqlLockMode)99;
        Action act = () => "SELECT * FROM orders".WithMySqlLock(invalidMode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lockMode")
            .WithMessage("*Unsupported MySQL lock mode.*");
    }
}
