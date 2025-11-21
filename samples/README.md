# Kommand Samples

This directory contains sample projects demonstrating how to use the Kommand CQRS/Mediator library.

## Solution: KommandSamples.sln

The `KommandSamples.sln` solution includes all sample projects:

- **Kommand.Sample** - Uses project reference to `src/Kommand/Kommand.csproj`
  - For development and testing against local changes
  - Automatically picks up latest library changes

- **Kommand.Sample.NuGet** - Uses Kommand NuGet package (v1.0.0-alpha.1)
  - Demonstrates end-user consumption of Kommand from NuGet
  - Shows realistic integration experience

## Building the Samples

```bash
# Build all samples
dotnet build KommandSamples.sln

# Build in Release mode
dotnet build KommandSamples.sln --configuration Release
```

## Running the Samples

### Kommand.Sample (Project Reference)

```bash
cd Kommand.Sample
dotnet run
```

### Kommand.Sample.NuGet (NuGet Package)

```bash
cd Kommand.Sample.NuGet
dotnet restore  # Downloads Kommand from NuGet
dotnet run
```

## Sample Features

Both samples demonstrate:

✅ **CQRS** - Commands and queries with explicit separation
✅ **Validation** - Async validation with database checks
✅ **Notifications** - Pub/sub domain events with multiple handlers
✅ **Interceptors** - Custom logging and built-in OpenTelemetry
✅ **Dependency Injection** - Auto-discovery of handlers and validators

See individual project READMEs for detailed documentation:
- [Kommand.Sample/README.md](Kommand.Sample/README.md)
- [Kommand.Sample.NuGet/README.md](Kommand.Sample.NuGet/README.md)

## Prerequisites

- .NET 10.0 SDK or later
- For NuGet sample: Internet connection to download packages
