# Benchmarks

The goal of the benchmark is to compare the performance of the FHIR Anonymizer CLI over a set of json files by using a plain configuration, and another with Presdio integration.

The benchmarks are implemented using [BenchmarkDotNet](https://benchmarkdotnet.org/)

## Setup

1. Copy the two configuration files from `config/` to `c:\benchmark\config (win)` or `/benchmark/config (linux)`
2. Copy `input` to `c:\benchmark\input` or `/benchmark/input`
3. Run the benchmark:

```
# choose the jobtype: dry/short/medium/long

# from the root of the repo:
$> cd benchmark\src\Microsoft.Health.Fhir.Anonymizer.Benchmarks
$> dotnet run -c Release -- --job <jobtype> --filter *FullFlow* --runtimes netcoreapp3.1
```

## Docker

There is a docker file available, and can be used as follows. For this, ensure the `\<repo-root\>/benchmark/input` and `\<repo-root\>/benchmark/config` have valid files.

```
# from the root of the repo:
$> docker build -t fhircli-benchmark -f benchmark/Dockerfile .
$> docker run -t fhircli-benchmark --job <jobtype> --filter *FullFlow* --runtimes netcoreapp3.1
```
