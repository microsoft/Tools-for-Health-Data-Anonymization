# Benchmarks

The goal of the benchmark is to compare the performance of the FHIR Anonymizer CLI over a set of json files by using a plain configuration, and another with Presdio integration. There are 2 configs available in the `config` folder. The config `configuration-sample-presidio.json` expects a local instance of Presidio to run. [More information on how to run Presidio](https://microsoft.github.io/presidio/installation/#using-docker).

The benchmarks are implemented using [BenchmarkDotNet](https://benchmarkdotnet.org/).

## Running the benchmark locally

### Prerequisites

The `presidio-analyzer` and `presidio-anonymizer` docker container need to be running and listening on port 5001 and 5002.

### Manually

1. Copy the two configuration files from `config/` to `c:\benchmark\config (win)` or `/benchmark/config (linux)`
2. Copy `input` (or use your own set of input files) to `c:\benchmark\input` or `/benchmark/input`
3. Run the benchmark from command line:

```powershell
# choose the jobtype: dry/short/medium/long
# from the root of the repo:
$> cd benchmark\src\Microsoft.Health.Fhir.Anonymizer.Benchmarks
$> dotnet run -c Release -- --job <jobtype> --filter *FullFlow* --runtimes netcoreapp3.1
```

> Note: A `long` run is 100 iterations over 3 starts, and will take up to 10 hours to complete, depending on the hardware.  
>
> It is possible to adjust the runtimes and expected outputs. [More information](https://benchmarkdotnet.org/articles/guides/console-args.html).

### Docker

There is a Dockerfile available, that allows the benchmarks to be run from a container. Ensure the `\<repo-root\>/benchmark/input` and `\<repo-root\>/benchmark/config` have valid files.

```powershell
# from the root of the repo:
$> docker build -t fhircli-benchmark -f benchmark/Dockerfile .
$> docker run --privileged -it fhircli-benchmark --job <jobtype> --filter *FullFlow* --runtimes netcoreapp3.1
```

#### Getting the results from the container

You can access the results by mounting a local folder when running the container:

```powershell
 $> docker run --privileged --mount src=c:/fhircli/dockerout,target=/benchmark/src/Microsoft.Health.Fhir.Anonymizer.Stu3.Benchmarks/BenchmarkDotNet.Artifacts,type=bind fhircli-benchmark --job dry --filter *FullFlow*
```

- `src`: the folder on the host machine that should be mounted. Ensure the directory exists.
- `target`: the folder where BenchmarkDotNet stores the results by default.

### Docker Compose

There is a `docker-compose.yml` in the root of the repository, which can be used as follows:

```powershell
# set the environment variables manually
$> $env:JOB_TYPE='dry'
$> $env:ARTIFACT_PATH='<path-to-artifacts>'
$> docker compose build
$> docker compose up
```

## ADO Pipeline

There is a pipeline definition available in the `.pipelines` folder.
