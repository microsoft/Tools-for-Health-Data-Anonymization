FROM mcr.microsoft.com/dotnet/core/sdk:3.1

ENV FHIRCLIBENCHMARKROOT=/benchmark

COPY . .
RUN dotnet build -c Release -o bin
COPY /benchmark/config /bin/benchmark/config/
COPY /benchmark/input /bin/benchmark/input/

ENTRYPOINT [ "dotnet", "bin/Microsoft.Health.Fhir.Anonymizer.Stu3.Benchmarks.dll"]

