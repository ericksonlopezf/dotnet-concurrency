// Copyright © Erickson Lopez. MIT License.
using System;
using AwesomeAssertions;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.AspNetCore.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace EricksonLopez.Concurrency.AspNetCore.Tests;

public sealed class ConcurrencyHttpExtensionsTests
{
    private sealed record StubToken(string Value) : IConcurrencyToken
    {
        public string TokenKind => "Stub";
        public bool IsEmpty => string.IsNullOrEmpty(Value);
        public bool Equals(IConcurrencyToken? other) => other is not null && Value == other.Value;
    }

    [Fact]
    public void GetExpectedConcurrencyToken_NullRequest_ShouldThrowArgumentNullException()
    {
        HttpRequest nullRequest = null!;
        Action act = () => nullRequest.GetExpectedConcurrencyToken();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void GetExpectedConcurrencyToken_Headers_ShouldExtractNormalizedToken()
    {
        // If-Match with strong ETag
        var context1 = new DefaultHttpContext();
        context1.Request.Headers[HeaderNames.IfMatch] = "\"strong-token-1\"";
        context1.Request.GetExpectedConcurrencyToken()!.Value.Value.Should().Be("strong-token-1");

        // If-Match with weak ETag
        var context2 = new DefaultHttpContext();
        context2.Request.Headers[HeaderNames.IfMatch] = "W/\"weak-token-2\"";
        context2.Request.GetExpectedConcurrencyToken()!.Value.Value.Should().Be("weak-token-2");

        // If-None-Match fallback when If-Match empty
        var context3 = new DefaultHttpContext();
        context3.Request.Headers[HeaderNames.IfNoneMatch] = "\"none-match-token\"";
        context3.Request.GetExpectedConcurrencyToken()!.Value.Value.Should().Be("none-match-token");

        // Raw unquoted trimmed token
        var context4 = new DefaultHttpContext();
        context4.Request.Headers[HeaderNames.IfMatch] = " raw-token ";
        context4.Request.GetExpectedConcurrencyToken()!.Value.Value.Should().Be("raw-token");

        // Unclosed and unopened quote strings
        var context5 = new DefaultHttpContext();
        context5.Request.Headers[HeaderNames.IfMatch] = "\"unclosed-quote";
        context5.Request.GetExpectedConcurrencyToken()!.Value.Value.Should().Be("\"unclosed-quote");

        var context6 = new DefaultHttpContext();
        context6.Request.Headers[HeaderNames.IfMatch] = "unopened-quote\"";
        context6.Request.GetExpectedConcurrencyToken()!.Value.Value.Should().Be("unopened-quote\"");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"\"")]
    [InlineData("W/\"\"")]
    public void GetExpectedConcurrencyToken_EmptyOrInvalidHeaders_ShouldReturnNull(string? headerValue)
    {
        var context = new DefaultHttpContext();
        if (headerValue is not null)
        {
            context.Request.Headers[HeaderNames.IfMatch] = headerValue;
        }

        context.Request.GetExpectedConcurrencyToken().Should().BeNull();
    }

    [Fact]
    public void GetExpectedConcurrencyVersion_NullRequest_ShouldThrowArgumentNullException()
    {
        HttpRequest nullRequest = null!;
        Action act = () => nullRequest.GetExpectedConcurrencyVersion();

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void GetExpectedConcurrencyVersion_ValidNumericVersion_ShouldReturnExpectedVersion()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderNames.IfMatch] = "\"42\"";

        ExpectedVersion? version = context.Request.GetExpectedConcurrencyVersion();
        version.Should().Be(ExpectedVersion.Specific(42));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("\"non-numeric-token\"")]
    [InlineData("\"-5\"")]
    public void GetExpectedConcurrencyVersion_NonNumericOrMissing_ShouldReturnNull(string? headerValue)
    {
        var context = new DefaultHttpContext();
        if (headerValue is not null)
        {
            context.Request.Headers[HeaderNames.IfMatch] = headerValue;
        }

        context.Request.GetExpectedConcurrencyVersion().Should().BeNull();
    }

    [Fact]
    public void SetConcurrencyETag_NullResponse_ShouldThrowArgumentNullException()
    {
        HttpResponse nullResponse = null!;
        var token = new StubToken("val");
        var valToken = ConcurrencyToken.From("val");
        var version = ConcurrencyVersion.From(1);

        Action act1 = () => nullResponse.SetConcurrencyETag(token);
        act1.Should().Throw<ArgumentNullException>().WithParameterName("response");

        Action act2 = () => nullResponse.SetConcurrencyETag(valToken);
        act2.Should().Throw<ArgumentNullException>().WithParameterName("response");

        Action act3 = () => nullResponse.SetConcurrencyETag(version);
        act3.Should().Throw<ArgumentNullException>().WithParameterName("response");

        Action act4 = () => nullResponse.SetConcurrencyETag(10L);
        act4.Should().Throw<ArgumentNullException>().WithParameterName("response");
    }

    [Fact]
    public void SetConcurrencyETag_NullToken_ShouldThrowArgumentNullException()
    {
        var context = new DefaultHttpContext();
        IConcurrencyToken nullToken = null!;

        Action act = () => context.Response.SetConcurrencyETag(nullToken);
        act.Should().Throw<ArgumentNullException>().WithParameterName("token");
    }

    [Fact]
    public void SetConcurrencyETag_EmptyToken_ShouldNotSetHeader()
    {
        var context = new DefaultHttpContext();
        var emptyToken = new StubToken("");

        context.Response.SetConcurrencyETag(emptyToken);
        context.Response.Headers.Should().NotContainKey(HeaderNames.ETag);
    }

    [Fact]
    public void SetConcurrencyETag_AllOverloads_ShouldSetExpectedHeader()
    {
        // IConcurrencyToken weak & strong
        var ctx1 = new DefaultHttpContext();
        ctx1.Response.SetConcurrencyETag(new StubToken("tok-1"), isWeak: true);
        ctx1.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"tok-1\"");

        var ctx2 = new DefaultHttpContext();
        ctx2.Response.SetConcurrencyETag(new StubToken("tok-2"), isWeak: false);
        ctx2.Response.Headers[HeaderNames.ETag].ToString().Should().Be("\"tok-2\"");

        // ConcurrencyToken struct weak & strong
        var ctx3 = new DefaultHttpContext();
        ctx3.Response.SetConcurrencyETag(ConcurrencyToken.From("tok-3"), isWeak: true);
        ctx3.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"tok-3\"");

        var ctx4 = new DefaultHttpContext();
        ctx4.Response.SetConcurrencyETag(ConcurrencyToken.From("tok-4"), isWeak: false);
        ctx4.Response.Headers[HeaderNames.ETag].ToString().Should().Be("\"tok-4\"");

        // ConcurrencyVersion struct weak & strong
        var ctx5 = new DefaultHttpContext();
        ctx5.Response.SetConcurrencyETag(ConcurrencyVersion.From(100), isWeak: true);
        ctx5.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"100\"");

        var ctx6 = new DefaultHttpContext();
        ctx6.Response.SetConcurrencyETag(ConcurrencyVersion.From(200), isWeak: false);
        ctx6.Response.Headers[HeaderNames.ETag].ToString().Should().Be("\"200\"");

        // long version weak & strong
        var ctx7 = new DefaultHttpContext();
        ctx7.Response.SetConcurrencyETag(300L, isWeak: true);
        ctx7.Response.Headers[HeaderNames.ETag].ToString().Should().Be("W/\"300\"");

        var ctx8 = new DefaultHttpContext();
        ctx8.Response.SetConcurrencyETag(400L, isWeak: false);
        ctx8.Response.Headers[HeaderNames.ETag].ToString().Should().Be("\"400\"");
    }
}
