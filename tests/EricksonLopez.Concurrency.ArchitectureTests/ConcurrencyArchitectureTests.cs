// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Dapper;
using EricksonLopez.Concurrency.MariaDb;
using EricksonLopez.Concurrency.Mediator;
using EricksonLopez.Concurrency.MySql;
using EricksonLopez.Concurrency.Oracle;
using EricksonLopez.Concurrency.PostgreSql;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Concurrency.SqlServer;
using EricksonLopez.Concurrency.Sqlite;
using NetArchTest.Rules;
using Xunit;

namespace EricksonLopez.Concurrency.ArchitectureTests;

public sealed class ConcurrencyArchitectureTests
{
    private static readonly Assembly AbstractionsAssembly = typeof(IConcurrencyToken).Assembly;
    private static readonly Assembly CoreAssembly = typeof(ConcurrencyController).Assembly;
    private static readonly Assembly DapperAssembly = typeof(ConcurrencyDapperExtensions).Assembly;
    private static readonly Assembly PostgreSqlAssembly = typeof(PostgreSqlConcurrencyErrorClassifier).Assembly;
    private static readonly Assembly SqlServerAssembly = typeof(SqlServerErrorClassifier).Assembly;
    private static readonly Assembly MySqlAssembly = typeof(MySqlConcurrencyErrorClassifier).Assembly;
    private static readonly Assembly MariaDbAssembly = typeof(MariaDbConcurrencyErrorClassifier).Assembly;
    private static readonly Assembly OracleAssembly = typeof(OracleConcurrencyErrorClassifier).Assembly;
    private static readonly Assembly SqliteAssembly = typeof(SqliteConcurrencyErrorClassifier).Assembly;
    private static readonly Assembly ResultAssembly = typeof(ConcurrencyResultExtensions).Assembly;
    private static readonly Assembly MediatorAssembly = typeof(ConcurrencyBehavior<,>).Assembly;
    private static readonly Assembly TestingAssembly = typeof(EricksonLopez.Concurrency.Testing.FakeConcurrencyController).Assembly;
    private static readonly Assembly AspNetCoreAssembly = typeof(EricksonLopez.Concurrency.AspNetCore.Models.ConcurrencyProblemDetails).Assembly;

    private static readonly Assembly[] AllSourceAssemblies =
    [
        AbstractionsAssembly,
        CoreAssembly,
        DapperAssembly,
        PostgreSqlAssembly,
        SqlServerAssembly,
        MySqlAssembly,
        MariaDbAssembly,
        OracleAssembly,
        SqliteAssembly,
        ResultAssembly,
        MediatorAssembly,
        TestingAssembly,
        AspNetCoreAssembly
    ];

    [Fact]
    public void Abstractions_ShouldNotDependOn_CoreOrAdapters()
    {
        TestResult result = Types.InAssembly(AbstractionsAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Concurrency.Controllers",
                "EricksonLopez.Concurrency.Diagnostics",
                "EricksonLopez.Concurrency.Resolvers",
                "EricksonLopez.Concurrency.Dapper",
                "EricksonLopez.Concurrency.PostgreSql",
                "EricksonLopez.Concurrency.SqlServer",
                "EricksonLopez.Concurrency.MySql",
                "EricksonLopez.Concurrency.MariaDb",
                "EricksonLopez.Concurrency.Oracle",
                "EricksonLopez.Concurrency.Sqlite",
                "EricksonLopez.Concurrency.Result",
                "EricksonLopez.Concurrency.Mediator",
                "Dapper",
                "Npgsql",
                "Microsoft.Data.SqlClient",
                "MySqlConnector",
                "Oracle.ManagedDataAccess.Client",
                "Microsoft.Data.Sqlite")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Core_ShouldNotDependOn_DapperOrDialectsOrMediator()
    {
        TestResult result = Types.InAssembly(CoreAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Concurrency.Dapper",
                "EricksonLopez.Concurrency.PostgreSql",
                "EricksonLopez.Concurrency.SqlServer",
                "EricksonLopez.Concurrency.MySql",
                "EricksonLopez.Concurrency.MariaDb",
                "EricksonLopez.Concurrency.Oracle",
                "EricksonLopez.Concurrency.Sqlite",
                "EricksonLopez.Concurrency.Result",
                "EricksonLopez.Concurrency.Mediator",
                "Dapper",
                "Npgsql",
                "Microsoft.Data.SqlClient",
                "MySqlConnector",
                "Oracle.ManagedDataAccess.Client",
                "Microsoft.Data.Sqlite")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void Dapper_ShouldNotDependOn_SpecificDatabaseDrivers()
    {
        TestResult result = Types.InAssembly(DapperAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Concurrency.PostgreSql",
                "EricksonLopez.Concurrency.SqlServer",
                "EricksonLopez.Concurrency.MySql",
                "EricksonLopez.Concurrency.MariaDb",
                "EricksonLopez.Concurrency.Oracle",
                "EricksonLopez.Concurrency.Sqlite",
                "Npgsql",
                "Microsoft.Data.SqlClient",
                "MySqlConnector",
                "Oracle.ManagedDataAccess.Client",
                "Microsoft.Data.Sqlite")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void DialectAdapters_ShouldNotDependOn_Dapper()
    {
        Assembly[] dialectAssemblies =
        [
            PostgreSqlAssembly,
            SqlServerAssembly,
            MySqlAssembly,
            MariaDbAssembly,
            OracleAssembly,
            SqliteAssembly
        ];

        foreach (Assembly asm in dialectAssemblies)
        {
            TestResult result = Types.InAssembly(asm)
                .ShouldNot()
                .HaveDependencyOnAny(
                    "EricksonLopez.Concurrency.Dapper",
                    "Dapper")
                .GetResult();

            result.IsSuccessful.Should().BeTrue();
        }
    }

    [Fact]
    public void ResultIntegration_ShouldNotDependOn_DapperOrDialects()
    {
        TestResult result = Types.InAssembly(ResultAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "EricksonLopez.Concurrency.Dapper",
                "EricksonLopez.Concurrency.PostgreSql",
                "EricksonLopez.Concurrency.SqlServer",
                "EricksonLopez.Concurrency.MySql",
                "EricksonLopez.Concurrency.MariaDb",
                "EricksonLopez.Concurrency.Oracle",
                "EricksonLopez.Concurrency.Sqlite",
                "Dapper",
                "Npgsql",
                "Microsoft.Data.SqlClient",
                "MySqlConnector",
                "Oracle.ManagedDataAccess.Client",
                "Microsoft.Data.Sqlite")
            .GetResult();

        result.IsSuccessful.Should().BeTrue();
    }

    [Fact]
    public void AllAssemblies_ShouldHaveStandardEricksonLopezPrefixes()
    {
        AbstractionsAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Abstractions");
        CoreAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency");
        DapperAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Dapper");
        PostgreSqlAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.PostgreSql");
        SqlServerAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.SqlServer");
        MySqlAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.MySql");
        MariaDbAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.MariaDb");
        OracleAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Oracle");
        SqliteAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Sqlite");
        ResultAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Result");
        MediatorAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Mediator");
        TestingAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.Testing");
        AspNetCoreAssembly.GetName().Name.Should().Be("EricksonLopez.Concurrency.AspNetCore");
    }

    [Fact]
    public void AllSourceAssemblies_MustNotContainObsoleteTypesOrMembers()
    {
        foreach (Assembly asm in AllSourceAssemblies)
        {
            var obsoleteTypes = asm.GetTypes()
                .Where(t => t.GetCustomAttribute<ObsoleteAttribute>() is not null)
                .Select(t => t.FullName)
                .ToList();

            obsoleteTypes.Should().BeEmpty($"Assembly {asm.GetName().Name} contains [Obsolete] types: {string.Join(", ", obsoleteTypes)}");

            var obsoleteMembers = asm.GetTypes()
                .SelectMany(t => t.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                .Where(m => m.GetCustomAttribute<ObsoleteAttribute>() is not null)
                .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
                .ToList();

            obsoleteMembers.Should().BeEmpty($"Assembly {asm.GetName().Name} contains [Obsolete] members: {string.Join(", ", obsoleteMembers)}");
        }
    }

    [Fact]
    public void AllMarkdownDocFiles_MustUseKebabCaseNaming_UnlessReserved()
    {
        string solutionRoot = FindSolutionRoot();
        string[] reservedNames =
        [
            "README.md",
            "CHANGELOG.md",
            "CODE_OF_CONDUCT.md",
            "CONTRIBUTING.md",
            "SECURITY.md",
            "SUPPORT.md",
            "LICENSE",
            "PULL_REQUEST_TEMPLATE.md"
        ];

        var mdFiles = Directory.GetFiles(solutionRoot, "*.md", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + ".git" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "node_modules" + Path.DirectorySeparatorChar))
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name))
            .Where(name => !reservedNames.Contains(name, StringComparer.Ordinal))
            .Where(name => name!.Any(char.IsUpper) || name!.Contains('_', StringComparison.Ordinal))
            .ToList();

        mdFiles.Should().BeEmpty($"All non-reserved markdown files must use kebab-case: {string.Join(", ", mdFiles)}");
    }

    [Fact]
    public void AllProductionSourceFiles_MustSatisfyOneTypePerFile()
    {
        string solutionRoot = FindSolutionRoot();
        string srcPath = Path.Combine(solutionRoot, "src");

        var csFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        csFiles.Should().NotBeEmpty();

        var multiTypeFiles = new System.Collections.Generic.List<string>();
        foreach (string file in csFiles)
        {
            var lines = File.ReadAllLines(file)
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .ToList();

            int topLevelTypes = 0;
            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^(public|internal|private|protected)?\s*(sealed|abstract|static|readonly)?\s*(class|struct|record|interface|enum)\s+[A-Za-z0-9_]+"))
                {
                    // Check if it is top-level (not indented deeply as a nested type)
                    if (!line.StartsWith("        ", StringComparison.Ordinal) && !line.StartsWith("\t\t", StringComparison.Ordinal))
                    {
                        topLevelTypes++;
                    }
                }
            }

            if (topLevelTypes > 1)
            {
                multiTypeFiles.Add(Path.GetFileName(file));
            }
        }

        multiTypeFiles.Should().BeEmpty($"All production files in src/ must contain only one top-level type: {string.Join(", ", multiTypeFiles)}");
    }

    [Fact]
    public void AllCSharpSourceFiles_MustContainMitLicenseHeader()
    {
        string solutionRoot = FindSolutionRoot();
        string srcPath = Path.Combine(solutionRoot, "src");

        var csFiles = Directory.GetFiles(srcPath, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                        !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
            .ToList();

        csFiles.Should().NotBeEmpty();

        const string expectedHeader = "// Copyright © Erickson Lopez. MIT License.";
        var invalidFiles = csFiles
            .Where(f =>
            {
                string firstLine = File.ReadLines(f).FirstOrDefault() ?? string.Empty;
                return firstLine.Trim() != expectedHeader;
            })
            .Select(Path.GetFileName)
            .ToList();

        invalidFiles.Should().BeEmpty($"All source .cs files must have the standard MIT license header: {string.Join(", ", invalidFiles)}");
    }

    private static string FindSolutionRoot()
    {
        string dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "EricksonLopez.Concurrency.slnx")) ||
                File.Exists(Path.Combine(dir, "Directory.Packages.props")))
            {
                return dir;
            }

            dir = Directory.GetParent(dir)?.FullName ?? string.Empty;
        }

        return AppDomain.CurrentDomain.BaseDirectory;
    }
}
