FROM mcr.microsoft.com/dotnet/core/sdk:3.1

COPY benchmark/config /fhircli/benchmark/config/
COPY benchmark/input /fhircli/benchmark/input/

# copy all the code, as it is needed to run the benchmark
# benchmarkdotnet will rebuild the code
COPY benchmark/src /benchmark/src/
COPY src /src/

WORKDIR /benchmark/src/Microsoft.Health.Fhir.Anonymizer.Stu3.Benchmarks

RUN dotnet build -c Release -o bin

ENTRYPOINT ["dotnet", "bin/Microsoft.Health.Fhir.Anonymizer.Stu3.Benchmarks.dll"]

