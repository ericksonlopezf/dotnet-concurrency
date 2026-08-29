// Copyright © Erickson Lopez. MIT License.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace EricksonLopez.Concurrency.AspNetCore.Tests;

public sealed class ConcurrencyConflictMiddlewareTests
{
    private sealed record StubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    private sealed class StubLogger<T> : ILogger<T>
    {
        public readonly List<(LogLevel Level, string Message)> Logs = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Logs.Add((logLevel, formatter(state, exception)));
        }
    }

    [Fact]
    public void Constructor_NullNext_ShouldThrowArgumentNullException()
    {
        Action act = () => _ = new ConcurrencyConflictMiddleware(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("next");
    }

    [Fact]
    public async Task InvokeAsync_NullContext_ShouldThrowArgumentNullException()
    {
        var middleware = new ConcurrencyConflictMiddleware(ctx => Task.CompletedTask);
        Func<Task> act = async () => await middleware.InvokeAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public async Task InvokeAsync_Success_ShouldCallNextWithoutInterference()
    {
        bool nextCalled = false;
        var middleware = new ConcurrencyConflictMiddleware(ctx =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        var context = new DefaultHttpContext();
        await middleware.InvokeAsync(context);

        nextCalled.Should().BeTrue();
    }

    private sealed class StartedResponseFeature : Microsoft.AspNetCore.Http.Features.IHttpResponseFeature
    {
        public int StatusCode { get; set; } = 200;
        public string? ReasonPhrase { get; set; }
        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();
        public Stream Body { get; set; } = new MemoryStream();
        public bool HasStarted => true;
        public void OnStarting(Func<object, Task> callback, object state) { }
        public void OnCompleted(Func<object, Task> callback, object state) { }
    }

    [Fact]
    public async Task InvokeAsync_ConcurrencyException_ResponseAlreadyStarted_ShouldLogWarningAndRethrow()
    {
        var logger = new StubLogger<ConcurrencyConflictMiddleware>();
        var exception = new ConcurrencyException("Conflict when response already started");

        var middleware = new ConcurrencyConflictMiddleware(ctx => throw exception, logger);

        var context = new DefaultHttpContext();
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());

        Func<Task> act = async () => await middleware.InvokeAsync(context);

        await act.Should().ThrowAsync<ConcurrencyException>()
            .WithMessage("Conflict when response already started");

        logger.Logs.Should().ContainSingle(l => l.Level == LogLevel.Warning);
        logger.Logs[0].Message.Should().Contain("ConcurrencyException was thrown but HTTP response has already started.");

        // Without logger
        var middlewareNoLogger = new ConcurrencyConflictMiddleware(ctx => throw exception, null);
        var context2 = new DefaultHttpContext();
        context2.Features.Set<Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>(new StartedResponseFeature());

        Func<Task> act2 = async () => await middlewareNoLogger.InvokeAsync(context2);
        await act2.Should().ThrowAsync<ConcurrencyException>();
    }

    [Fact]
    public async Task InvokeAsync_ConcurrencyException_WithActualToken_ShouldWriteETagAndProblemDetails()
    {
        var logger = new StubLogger<ConcurrencyConflictMiddleware>();
        var token = new StubToken("token-v123");
        var conflict = new ConcurrencyConflict(
            entityId: "e1",
            entityType: "Account",
            conflictType: ConcurrencyConflictType.TokenMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "PUT",
            message: "Token mismatch on account",
            actualToken: token);
        var exception = new ConcurrencyException(conflict);

        var middleware = new ConcurrencyConflictMiddleware(ctx => throw exception, logger);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        context.Response.Headers.Append("X-Initial-Header", "pre-existing");
        context.Request.Method = "PUT";
        context.Request.Path = "/api/accounts/e1";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"token-v123\"");
        context.Response.Headers.Should().NotContainKey("X-Initial-Header");
        logger.Logs.Should().ContainSingle(l => l.Level == LogLevel.Information);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
        doc.RootElement.GetProperty("conflictType").GetString().Should().Be("TokenMismatch");
    }

    [Fact]
    public async Task InvokeAsync_ConcurrencyException_WithActualVersion_ShouldWriteETag()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "p1",
            entityType: "Product",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "POST",
            message: "Version mismatch on product",
            actualVersion: ActualVersion.From(88));
        var exception = new ConcurrencyException(conflict);

        var middleware = new ConcurrencyConflictMiddleware(ctx => throw exception, null);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        context.Request.Method = "POST";
        context.Request.Path = "/api/products/p1";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"88\"");
    }

    [Fact]
    public async Task InvokeAsync_ConcurrencyException_WithEmptyActualTokenAndActualVersionNotFound_ShouldNotWriteETag()
    {
        var conflict = new ConcurrencyConflict(
            entityId: "p2",
            entityType: "Product",
            conflictType: ConcurrencyConflictType.VersionMismatch,
            classification: ConcurrencyConflictClassification.Transient,
            operation: "POST",
            message: "Version not found",
            actualToken: new StubToken(""),
            actualVersion: ActualVersion.NotFound);
        var exception = new ConcurrencyException(conflict);

        var middleware = new ConcurrencyConflictMiddleware(ctx => throw exception, null);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        context.Request.Method = "POST";
        context.Request.Path = "/api/products/p2";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.Headers.Should().NotContainKey(HeaderNames.ETag);
    }

    [Fact]
    public async Task InvokeAsync_ConcurrencyException_WithoutConflict_ShouldFallbackToSyntheticConflict()
    {
        var exception = new ConcurrencyException("Direct message without conflict object");

        var middleware = new ConcurrencyConflictMiddleware(ctx => throw exception, null);

        var serviceProvider = new ServiceCollection()
            .AddProblemDetails()
            .AddLogging()
            .BuildServiceProvider();

        var context = new DefaultHttpContext { RequestServices = serviceProvider };
        context.Response.Body = new MemoryStream();
        context.Request.Method = "PATCH";
        context.Request.Path = "/api/items/unknown";

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        string json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(409);
        doc.RootElement.GetProperty("entityId").GetString().Should().Be("unknown");
        doc.RootElement.GetProperty("entityType").GetString().Should().Be("unknown");
        doc.RootElement.GetProperty("conflictType").GetString().Should().Be("VersionMismatch");
        doc.RootElement.GetProperty("classification").GetString().Should().Be("Transient");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("Direct message without conflict object");
    }
}
