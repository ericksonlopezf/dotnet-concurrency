// Copyright © Erickson Lopez. MIT License.
using System;
using System.Text;

namespace EricksonLopez.Concurrency.Dapper;

/// <summary>
/// Provides helper methods for generating SQL UPDATE statements with optimistic concurrency and multi-tenancy predicates.
/// </summary>
public static class OptimisticUpdateBuilder
{
    /// <summary>
    /// Generates an optimistic version-incrementing SQL UPDATE command.
    /// </summary>
    /// <param name="tableName">The target database table name.</param>
    /// <param name="setClauses">The column assignment expressions (e.g., <c>name = @Name, status = @Status</c>).</param>
    /// <param name="idColumn">The name of the identifier column. Defaults to <c>"id"</c>.</param>
    /// <param name="versionColumn">The name of the concurrency version column. Defaults to <c>"version"</c>.</param>
    /// <param name="idParam">The name of the identifier parameter. Defaults to <c>"Id"</c>.</param>
    /// <param name="versionParam">The name of the expected version parameter. Defaults to <c>"ExpectedVersion"</c>.</param>
    /// <param name="tenantColumn">The optional name of the tenant isolation column.</param>
    /// <param name="tenantParam">The optional name of the tenant isolation parameter.</param>
    /// <returns>A generated SQL UPDATE statement containing optimistic concurrency predicates.</returns>
    /// <exception cref="ArgumentException"><paramref name="tableName"/>, <paramref name="setClauses"/>, <paramref name="idColumn"/>, or <paramref name="versionColumn"/> is <see langword="null"/> or whitespace or contains invalid identifier characters</exception>
    public static string BuildVersionedUpdate(
        string tableName,
        string setClauses,
        string idColumn = "id",
        string versionColumn = "version",
        string idParam = "Id",
        string versionParam = "ExpectedVersion",
        string? tenantColumn = null,
        string? tenantParam = null)
    {
        ValidateIdentifier(tableName, nameof(tableName));
        ArgumentException.ThrowIfNullOrWhiteSpace(setClauses);
        ValidateIdentifier(idColumn, nameof(idColumn));
        ValidateIdentifier(versionColumn, nameof(versionColumn));
        ValidateIdentifier(idParam, nameof(idParam));
        ValidateIdentifier(versionParam, nameof(versionParam));

        if (!string.IsNullOrWhiteSpace(tenantColumn))
        {
            ValidateIdentifier(tenantColumn, nameof(tenantColumn));
        }

        if (!string.IsNullOrWhiteSpace(tenantParam))
        {
            ValidateIdentifier(tenantParam, nameof(tenantParam));
        }

        var sb = new StringBuilder();
        sb.Append("UPDATE ").Append(tableName).Append(" SET ");
        sb.Append(setClauses);
        sb.Append(", ").Append(versionColumn).Append(" = ").Append(versionColumn).Append(" + 1");
        sb.Append(" WHERE ").Append(idColumn).Append(" = @").Append(idParam);

        if (!string.IsNullOrWhiteSpace(tenantColumn))
        {
            string tp = string.IsNullOrWhiteSpace(tenantParam) ? "TenantId" : tenantParam;
            sb.Append(" AND ").Append(tenantColumn).Append(" = @").Append(tp);
        }

        sb.Append(" AND ").Append(versionColumn).Append(" = @").Append(versionParam).Append(';');
        return sb.ToString();
    }

    private static void ValidateIdentifier(string identifier, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, paramName);
        foreach (char c in identifier)
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '.')
            {
                throw new ArgumentException($"Identifier '{identifier}' contains invalid characters.", paramName);
            }
        }
    }
}
