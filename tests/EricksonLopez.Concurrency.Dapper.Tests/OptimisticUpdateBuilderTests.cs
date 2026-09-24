// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Dapper;
using Xunit;

namespace EricksonLopez.Concurrency.Dapper.Tests;

public sealed class OptimisticUpdateBuilderTests
{
    [Fact]
    public void BuildVersionedUpdate_WithDefaults_ShouldGenerateCorrectSql()
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "orders",
            setClauses: "status = @Status, total = @Total");

        sql.Should().Be("UPDATE orders SET status = @Status, total = @Total, version = version + 1 WHERE id = @Id AND version = @ExpectedVersion;");
    }

    [Fact]
    public void BuildVersionedUpdate_WithCustomColumnsAndParameters_ShouldGenerateCorrectSql()
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "inventory_items",
            setClauses: "qty = @Qty",
            idColumn: "item_id",
            versionColumn: "rev",
            idParam: "ItemId",
            versionParam: "ExpectedRev");

        sql.Should().Be("UPDATE inventory_items SET qty = @Qty, rev = rev + 1 WHERE item_id = @ItemId AND rev = @ExpectedRev;");
    }

    [Fact]
    public void BuildVersionedUpdate_WithTenantColumn_AndDefaultTenantParam_ShouldGenerateTenantPredicate()
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "customers",
            setClauses: "name = @Name",
            tenantColumn: "tenant_id",
            tenantParam: null);

        sql.Should().Be("UPDATE customers SET name = @Name, version = version + 1 WHERE id = @Id AND tenant_id = @TenantId AND version = @ExpectedVersion;");

        string sqlEmptyParam = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "customers",
            setClauses: "name = @Name",
            tenantColumn: "tenant_id",
            tenantParam: "   ");

        sqlEmptyParam.Should().Be("UPDATE customers SET name = @Name, version = version + 1 WHERE id = @Id AND tenant_id = @TenantId AND version = @ExpectedVersion;");
    }

    [Fact]
    public void BuildVersionedUpdate_WithTenantColumn_AndCustomTenantParam_ShouldGenerateTenantPredicate()
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "customers",
            setClauses: "name = @Name",
            tenantColumn: "organization_id",
            tenantParam: "OrgId");

        sql.Should().Be("UPDATE customers SET name = @Name, version = version + 1 WHERE id = @Id AND organization_id = @OrgId AND version = @ExpectedVersion;");
    }

    [Theory]
    [InlineData(null, "status = @Status", "id", "version", "tableName")]
    [InlineData("", "status = @Status", "id", "version", "tableName")]
    [InlineData("   ", "status = @Status", "id", "version", "tableName")]
    [InlineData("orders", null, "id", "version", "setClauses")]
    [InlineData("orders", "", "id", "version", "setClauses")]
    [InlineData("orders", "   ", "id", "version", "setClauses")]
    [InlineData("orders", "status = @Status", null, "version", "idColumn")]
    [InlineData("orders", "status = @Status", "", "version", "idColumn")]
    [InlineData("orders", "status = @Status", "   ", "version", "idColumn")]
    [InlineData("orders", "status = @Status", "id", null, "versionColumn")]
    [InlineData("orders", "status = @Status", "id", "", "versionColumn")]
    [InlineData("orders", "status = @Status", "id", "   ", "versionColumn")]
    public void BuildVersionedUpdate_InvalidArguments_ShouldThrowArgumentException(
        string? tableName,
        string? setClauses,
        string? idColumn,
        string? versionColumn,
        string expectedParam)
    {
        Action act = () => OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: tableName!,
            setClauses: setClauses!,
            idColumn: idColumn!,
            versionColumn: versionColumn!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName(expectedParam);
    }

    [Fact]
    public void BuildVersionedUpdate_WithDotInTableName_ShouldSucceed()
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "dbo.orders",
            setClauses: "status = @Status");

        sql.Should().StartWith("UPDATE dbo.orders SET status = @Status");
    }

    [Fact]
    public void BuildVersionedUpdate_WithWhitespaceTenantColumn_ShouldNotIncludeTenantPredicate()
    {
        string sql = OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "orders",
            setClauses: "status = @Status",
            tenantColumn: "   ");

        sql.Should().Be("UPDATE orders SET status = @Status, version = version + 1 WHERE id = @Id AND version = @ExpectedVersion;");
    }

    [Fact]
    public void BuildVersionedUpdate_InvalidIdParam_ShouldThrowArgumentException()
    {
        Action act = () => OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "orders",
            setClauses: "status = @Status",
            idParam: "id;DROP");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("idParam")
            .WithMessage("Identifier 'id;DROP' contains invalid characters.*");
    }

    [Fact]
    public void BuildVersionedUpdate_InvalidVersionParam_ShouldThrowArgumentException()
    {
        Action act = () => OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "orders",
            setClauses: "status = @Status",
            versionParam: "ver-sion");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("versionParam")
            .WithMessage("Identifier 'ver-sion' contains invalid characters.*");
    }

    [Fact]
    public void BuildVersionedUpdate_InvalidTenantColumn_ShouldThrowArgumentException()
    {
        Action act = () => OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "orders",
            setClauses: "status = @Status",
            tenantColumn: "tenant-col!");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantColumn")
            .WithMessage("Identifier 'tenant-col!' contains invalid characters.*");
    }

    [Fact]
    public void BuildVersionedUpdate_InvalidTenantParam_ShouldThrowArgumentException()
    {
        Action act = () => OptimisticUpdateBuilder.BuildVersionedUpdate(
            tableName: "orders",
            setClauses: "status = @Status",
            tenantColumn: "tenant_id",
            tenantParam: "tenant-param");

        act.Should().Throw<ArgumentException>()
            .WithParameterName("tenantParam")
            .WithMessage("Identifier 'tenant-param' contains invalid characters.*");
    }
}
