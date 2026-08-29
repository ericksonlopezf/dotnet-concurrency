// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.PostgreSql;
using Xunit;

namespace EricksonLopez.Concurrency.PostgreSql.Tests;

public sealed class PostgreSqlLockExtensionsTests
{
    [Fact]
    public void ToSqlClause_AllValidModes_ShouldReturnCorrectClauses()
    {
        PostgreSqlLockMode.ForUpdate.ToSqlClause().Should().Be("FOR UPDATE");
        PostgreSqlLockMode.ForUpdateNoWait.ToSqlClause().Should().Be("FOR UPDATE NOWAIT");
        PostgreSqlLockMode.ForUpdateSkipLocked.ToSqlClause().Should().Be("FOR UPDATE SKIP LOCKED");
        PostgreSqlLockMode.ForShare.ToSqlClause().Should().Be("FOR SHARE");
        PostgreSqlLockMode.ForNoKeyUpdate.ToSqlClause().Should().Be("FOR NO KEY UPDATE");
    }

    [Fact]
    public void ToSqlClause_InvalidMode_ShouldThrowArgumentOutOfRangeException()
    {
        var invalidMode = (PostgreSqlLockMode)99;
        Action act = () => invalidMode.ToSqlClause();

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("mode")
            .WithMessage("*Unsupported PostgreSQL lock mode.*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithLock_NullOrWhiteSpace_ShouldThrowArgumentException(string? sql)
    {
        Action act = () => sql!.WithLock(PostgreSqlLockMode.ForUpdate);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("selectSql");
    }

    [Fact]
    public void WithLock_ShouldTrimSemicolonsAndSpaces_AndAppendClause()
    {
        string sqlWithSemicolon = "SELECT * FROM accounts WHERE id = @Id;";
        sqlWithSemicolon.WithLock(PostgreSqlLockMode.ForUpdate).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE;");

        string sqlWithSpacesAndSemicolon = "SELECT * FROM accounts WHERE id = @Id;   ";
        sqlWithSpacesAndSemicolon.WithLock(PostgreSqlLockMode.ForUpdateNoWait).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE NOWAIT;");

        string sqlWithoutSemicolon = "SELECT * FROM accounts WHERE id = @Id";
        sqlWithoutSemicolon.WithLock(PostgreSqlLockMode.ForShare).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR SHARE;");
        sqlWithoutSemicolon.WithLock(PostgreSqlLockMode.ForNoKeyUpdate).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR NO KEY UPDATE;");
        sqlWithoutSemicolon.WithLock(PostgreSqlLockMode.ForUpdateSkipLocked).Should().Be("SELECT * FROM accounts WHERE id = @Id FOR UPDATE SKIP LOCKED;");
    }
}
