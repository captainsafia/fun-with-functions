# File-Based Azure Functions with Aspire

This project demonstrates a file-based Azure Functions application orchestrated using Aspire. It showcases how to model and deploy Azure Functions as individual C# files within an Aspire distributed application.

## Overview

This application models file-based Azure Functions apps using a custom Aspire extension. Instead of traditional project-based Functions, each function is defined in a single .cs file with inline package references and dependencies. The Aspire app host orchestrates these functions along with their required infrastructure (Azure Storage).

The project includes two sample functions:

- `http.cs`: An HTTP-triggered function that responds to GET and POST requests
- `timer.cs`: A timer-triggered function that executes every 10 seconds

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (RC 2 or later)
- [Aspire 13](https://aspire.dev)

## Running the Application

### Local Development

Run the application locally using the Aspire orchestrator:

```bash
aspire run
```

This command will:

- Start the Aspire dashboard
- Launch the Azure Storage emulator (Azurite)
- Start both function apps (timer and http)
- Provide telemetry and logging through the Aspire dashboard

The Aspire dashboard will be available at `http://localhost:17193` (or another port if configured), where you can:

- Monitor function execution
- View logs and traces
- Inspect resource health
- Test HTTP endpoints

### Testing the HTTP Function

Once running, the HTTP function will be accessible through the Aspire dashboard's endpoints section. You can send requests to test it:

```bash
curl http://localhost:<assigned-port>/api/HttpTriggerImpl
```

## Deploying to Azure

Deploy the application to Azure Container Apps:

```bash
aspire deploy
```

This command will:
- Prompt you to select or create an Azure subscription
- Provision necessary Azure resources (Container App Environment, Storage Account)
- Build and deploy both function apps as containers to ACA Native Functions
- Configure networking and environment variables

After deployment, the command will output the URLs for your deployed functions.

## Project Structure

- `apphost.cs`: The Aspire app host that orchestrates the functions and infrastructure
- `http.cs`: HTTP-triggered function implementation
- `timer.cs`: Timer-triggered function implementation
- `host.json`: Azure Functions host configuration
- `local.settings.json`: Local development settings
- `global.json`: .NET SDK version specification

## File-Based Functions Model

This project uses a file-based approach to Azure Functions where:

1. Each function is defined in a single .cs file
2. Package references are declared inline using `#:package` directives
3. The Aspire `AddFunctionApp` extension method orchestrates the functions
4. Azure Storage is automatically provisioned and configured
5. OpenTelemetry integration is enabled for observability

### Custom Extension

The `FunctionsExtensions` class in `apphost.cs` provides the `AddFunctionApp` method that:

- Registers file-based function apps as Aspire resources
- Automatically provisions shared Azure Storage
- Configures Azure Functions runtime settings
- Enables OpenTelemetry for distributed tracing
- Sets up HTTP endpoints for both local and deployed scenarios=