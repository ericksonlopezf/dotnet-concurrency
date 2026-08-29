// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.SqlServer;
using Xunit;

namespace EricksonLopez.Concurrency.SqlServer.Tests;

public sealed class SqlServerLockExtensionsTests
{
    [Fact]
    public void WithSqlServerTableHint_AllValidModes_ShouldAppendHintProperly()
    {
        "Customers".WithSqlServerTableHint(SqlServerLockMode.UpdLockRowLock)
            .Should().Be("Customers WITH (UPDLOCK, ROWLOCK)");

        "Orders".WithSqlServerTableHint(SqlServerLockMode.UpdLockRowLockNowait)
            .Should().Be("Orders WITH (UPDLOCK, ROWLOCK, NOWAIT)");

        "Items".WithSqlServerTableHint(SqlServerLockMode.UpdLockRowLockReadPast)
            .Should().Be("Items WITH (UPDLOCK, ROWLOCK, READPAST)");

        "Accounts".WithSqlServerTableHint(SqlServerLockMode.XLockRowLock)
            .Should().Be("Accounts WITH (XLOCK, ROWLOCK)");

        "Invoices".WithSqlServerTableHint(SqlServerLockMode.XLockRowLockNowait)
            .Should().Be("Invoices WITH (XLOCK, ROWLOCK, NOWAIT)");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithSqlServerTableHint_NullOrWhiteSpace_ShouldThrowArgumentException(string? table)
    {
        Action act = () => table!.WithSqlServerTableHint(SqlServerLockMode.UpdLockRowLock);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("tableName");
    }

    [Fact]
    public void WithSqlServerTableHint_InvalidMode_ShouldThrowArgumentOutOfRangeException()
    {
        var invalidMode = (SqlServerLockMode)99;
        Action act = () => "Customers".WithSqlServerTableHint(invalidMode);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("lockMode")
            .WithMessage("*Unsupported SQL Server lock mode.*");
    }
}
