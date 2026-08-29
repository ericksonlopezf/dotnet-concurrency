// Copyright © Erickson Lopez. MIT License.
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using EricksonLopez.Concurrency.Abstractions;
using EricksonLopez.Concurrency.Controllers;
using EricksonLopez.Concurrency.Result;
using EricksonLopez.Result;

namespace EricksonLopez.Concurrency.Benchmarks;

[MemoryDiagnoser]
public class ConcurrencyBenchmarks
{
    public sealed class BenchEntity : IVersionedEntity
    {
        public string Id { get; init; } = "bench_1";
        public long Version { get; set; } = 10;
    }

    private readonly OptimisticConcurrencyChecker _checker = OptimisticConcurrencyChecker.Instance;
    private readonly ConcurrencyController _controller = new();
    private readonly ExpectedVersion _expectedVersion = ExpectedVersion.Specific(10);
    private readonly ConcurrencyVersion _actualVersion = new(10);
    private readonly ConcurrencyToken _tokenA = new("etag-12345", "ETag");
    private readonly ConcurrencyToken _tokenB = new("etag-12345", "ETag");
    private readonly BenchEntity _entity = new();

    [Benchmark(Baseline = true)]
    public bool DirectVersionComparison()
    {
        return _actualVersion.Value == 10;
    }

    [Benchmark]
    public bool CheckerCheckVersion()
    {
        return _checker.CheckVersion(_expectedVersion, _actualVersion, "bench_1", "BenchEntity", out _);
    }

    [Benchmark]
    public bool CheckerCheckToken()
    {
        return _checker.CheckToken(_tokenA, _tokenB, "bench_1", "BenchEntity", out _);
    }

    [Benchmark]
    public async ValueTask<CasResult<BenchEntity>> ControllerExecuteCasAsync()
    {
        return await _controller.ExecuteCasAsync(
            _entity,
            _expectedVersion,
            "bench_1",
            (ent, ct) => ValueTask.FromResult(ent),
            CancellationToken.None);
    }

    [Benchmark]
    public Result<BenchEntity> ResultConversion()
    {
        CasResult<BenchEntity> cas = CasResult.Succeeded(_entity, new ConcurrencyVersion(11));
        return cas.ToResult();
    }
}
