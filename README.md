# ZTR_OS

A .NET 9.0 operating system management platform with hardware abstraction, intelligence scheduling, and API services.

## Project Structure

```
ZTR_OS/
├── src/
│   ├── ZTR.Models/         # Models, enums, DTOs
│   ├── ZTR.Common/         # Utilities, helpers, interfaces
│   ├── ZTR.HAL/            # Hardware Abstraction Layer (ACPI, HID, GPU)
│   ├── ZTR.Intelligence/   # MLP engine, performance scheduler
│   └── ZTR.Api/            # ASP.NET Core Web API + SignalR
├── tests/
│   ├── ZTR.HAL.Tests/
│   ├── ZTR.Intelligence.Tests/
│   └── ZTR.Api.Tests/
├── Directory.Build.props
├── .editorconfig
├── .gitignore
└── ZTR_OS.sln
```

## Prerequisites

- .NET 9.0 SDK

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run --project src/ZTR.Api
```

## License

TBD
