// Copyright © Erickson Lopez. MIT License.
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace EricksonLopez.Concurrency.AspNetCore.Tests;

public sealed class ConcurrencyResultExtensionsTests
{
    private sealed record StubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void ConcurrencyConflictHttpResult_Constructor_NullConflict_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ConcurrencyConflictHttpResult(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("conflict");
    }

    [Fact]
    public void ConcurrencyConflictHttpResult_Properties_ShouldBeExposed()
    {
        var conflict = new ConcurrencyConflict("e1", "Entity", ConcurrencyConflictType.VersionMismatch, ConcurrencyConflictClassification.Transient, "Op", "Msg");
        var httpResult = new ConcurrencyConflictHttpResult(conflict, "/api/custom/path");

        httpResult.Conflict.Should().BeSameAs(conflict);
        httpResult.Instance.Should().Be("/api/custom/path");
    }

    [Fact]
    public async Task ConcurrencyConflictHttpResult_ExecuteAsync_NullContext_ShouldThrowArgumentNullException()
    {
        var conflict = new ConcurrencyConflict("e1", "Entity", ConcurrencyConflictType.VersionMismatch, ConcurrencyConflictClassification.Transient, "Op", "Msg");
        var httpResult = new ConcurrencyConflictHttpResult(conflict);

        Func<Task> act = async () => await httpResult.ExecuteAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("httpContext");
    }

    [Fact]
    public async Task ConcurrencyConflictHttpResult_ExecuteAsync_WithToken_ShouldWriteETagAndProblemDetails()
    {
        var token = new StubToken("token-v99");
        var conflict = new ConcurrencyConflict(
            entityId: "ent-1",
            entityType: "Account",
            conflictType: ConcurrencyConflictType.TokenMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "Transfer",
            message: "Token mismatch",
            actualToken: token);

        var httpResult = new ConcurrencyConflictHttpResult(conflict);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/accounts/ent-1";

        await httpResult.ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"token-v99\"");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
        doc.RootElement.GetProperty("conflictType").GetString().Should().Be("TokenMismatch");
        doc.RootElement.GetProperty("instance").GetString().Should().Be("/api/accounts/ent-1");
    }

    [Fact]
    public async Task ConcurrencyConflictHttpResult_ExecuteAsync_WithActualVersion_ShouldWriteETag()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "ent-2",
            entityType: "Product",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "Update",
            message: "Version mismatch",
            actualVersion: ActualVersion.From(42));

        var httpResult = new ConcurrencyConflictHttpResult(conflict, "/api/custom/instance");

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();

        await httpResult.ExecuteAsync(context);

        context.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"42\"");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("instance").GetString().Should().Be("/api/custom/instance");
    }

    [Fact]
    public async Task ConcurrencyConflictHttpResult_ExecuteAsync_WithActualVersionNotFound_ShouldNotWriteETag()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "ent-3",
            entityType: "Product",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "Update",
            message: "Version not found",
            actualVersion: ActualVersion.NotFound);

        var httpResult = new ConcurrencyConflictHttpResult(conflict);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();

        await httpResult.ExecuteAsync(context);

        context.Response.Headers.Should().NotContainKey(HeaderNames.ETag);
    }

    [Fact]
    public async Task ConcurrencyConflictHttpResult_ExecuteAsync_WithNeitherTokenNorVersion_ShouldNotWriteETag()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "ent-4",
            entityType: "Product",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "Update",
            message: "No token or version",
            expectedVersion: null,
            actualVersion: null,
            expectedToken: null,
            actualToken: null);

        var httpResult = new ConcurrencyConflictHttpResult(conflict);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();

        await httpResult.ExecuteAsync(context);

        context.Response.Headers.Should().NotContainKey(HeaderNames.ETag);
    }

    [Fact]
    public void ConcurrencyConflict_ExtensionMethod_NullGuards_ShouldThrowArgumentNullException()
    {
        IResultExtensions nullExtensions = null!;
        var conflict = new ConcurrencyConflict("e1", "Entity", ConcurrencyConflictType.VersionMismatch, ConcurrencyConflictClassification.Transient, "Op", "Msg");

        Action act1 = () => nullExtensions.ConcurrencyConflict(conflict);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("resultExtensions");

        Action act2 = () => Results.Extensions.ConcurrencyConflict(null!);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("conflict");
    }

    [Fact]
    public void ConcurrencyConflict_ExtensionMethod_Valid_ShouldReturnHttpResult()
    {
        var conflict = new ConcurrencyConflict("e1", "Entity", ConcurrencyConflictType.VersionMismatch, ConcurrencyConflictClassification.Transient, "Op", "Msg");
        IResult result = Results.Extensions.ConcurrencyConflict(conflict, "/api/inst");

        result.Should().BeOfType<ConcurrencyConflictHttpResult>();
        var httpResult = (ConcurrencyConflictHttpResult)result;
        httpResult.Conflict.Should().BeSameAs(conflict);
        httpResult.Instance.Should().Be("/api/inst");
    }
}
