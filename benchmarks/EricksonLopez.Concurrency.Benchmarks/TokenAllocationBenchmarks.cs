// Copyright © Erickson Lopez. MIT License.
using System;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Concurrency.Abstractions;

namespace EricksonLopez.Concurrency.Benchmarks;

[MemoryDiagnoser]
public class TokenAllocationBenchmarks
{
    private const string VersionString = "1234567890";
    private const string ETagString = "\"686897696a7c876b7e\"";
    private readonly ConcurrencyVersion _version = new(1234567890L);
    private readonly ConcurrencyToken _token = new("\"686897696a7c876b7e\"", "ETag");
    private readonly ExpectedVersion _expected = ExpectedVersion.Specific(1234567890L);

    [Benchmark(Baseline = true)]
    public ConcurrencyVersion CreateStructVersion()
    {
        return new ConcurrencyVersion(1234567890L);
    }

    [Benchmark]
    public ConcurrencyToken CreateStructToken()
    {
        return new ConcurrencyToken(ETagString, "ETag");
    }

    [Benchmark]
    public ExpectedVersion CreateExpectedVersionSpecific()
    {
        return ExpectedVersion.Specific(1234567890L);
    }

    [Benchmark]
    public bool TryParseSpanVersion()
    {
        ReadOnlySpan<char> span = VersionString.AsSpan();
        return ConcurrencyVersion.TryParse(span, null, out _);
    }

    [Benchmark]
    public bool TryParseStringVersion()
    {
        return ConcurrencyVersion.TryParse(VersionString, null, out _);
    }

    [Benchmark]
    public string FormatVersionToString()
    {
        return _version.ToString();
    }

    [Benchmark]
    public bool CompareTokensValueEquality()
    {
        var other = new ConcurrencyToken("\"686897696a7c876b7e\"", "ETag");
        return _token == other;
    }

    [Benchmark]
    public bool CheckExpectedMatchesActual()
    {
        return _expected.Matches(_version);
    }
}
