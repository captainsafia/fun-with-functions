#:package Aspire.Hosting.Azure.AppContainers@13.1.0-preview.1.25554.11
#:package Aspire.Hosting.Azure.Storage@13.1.0-preview.1.25554.11
#:package Aspire.Hosting.Python@13.1.0-preview.1.25554.11
#pragma warning disable ASPIRECSHARPAPPS001 // Type is for evaluation purposes only and is subject to change or 

#:sdk Aspire.AppHost.Sdk@13.1.0-preview.1.25554.11

using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("env");

builder.AddFunctionApp("timer", "timer.cs");
builder.AddFunctionApp("http", "http.cs")
    .WithExternalHttpEndpoints();

builder.Build().Run();

public static class FunctionsExtensions
{
    /// <summary>
    /// Adds a file-based Azure Functions app to the distributed application with Azure Storage dependency.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the function app resource.</param>
    /// <param name="path">The path to the file-based function .cs file.</param>
    /// <returns>A resource builder for the function app.</returns>
    public static IResourceBuilder<ProjectResource> AddFunctionApp(
        this IDistributedApplicationBuilder builder, 
        string name, 
        string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(path);

        // Get the storage resource name and look for existing resource
        var storageResourceName = CreateDefaultStorageName(builder);
        var existingStorage = builder.Resources
            .OfType<AzureStorageResource>()
            .FirstOrDefault(r => r.Name == storageResourceName);

        // Create the storage resource if it doesn't exist
        existingStorage ??= builder.AddAzureStorage(storageResourceName)
                .RunAsEmulator()
                .Resource;

        // Add the function app resource as a C# app with function-specific configurations
        var functionApp = builder.AddCSharpApp(name, path)
            .WithEnvironment(context =>
            {
                // Set Azure Functions specific environment variables
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES"] = "true";
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES"] = "true";
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY"] = "in_memory";
                context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                context.EnvironmentVariables["FUNCTIONS_WORKER_RUNTIME"] = "dotnet-isolated";
                
                // Required to enable OpenTelemetry in the Azure Functions host
                context.EnvironmentVariables["AzureFunctionsJobHost__telemetryMode"] = "OpenTelemetry";

                // Set the storage connection string using the existing storage
                ((IResourceWithAzureFunctionsConfig)existingStorage).ApplyAzureFunctionsConfiguration(context.EnvironmentVariables, "AzureWebJobsStorage");
                
                // Set ASPNETCORE_URLS for publish mode
                if (context.ExecutionContext.IsPublishMode)
                {
                    if (context.Resource is IResourceWithEndpoints resourceWithEndpoints)
                    {
                        var endpoint = resourceWithEndpoints.GetEndpoints().FirstOrDefault(e => e.EndpointAnnotation.UriScheme == "http");
                        if (endpoint != null)
                        {
                            context.EnvironmentVariables["ASPNETCORE_URLS"] = $"http://+:{endpoint.EndpointAnnotation.TargetPort ?? 8080}";
                        }
                    }
                }
            })
            .WithAnnotation(new AzureFunctionsAnnotation())
            .WithFunctionsHttpEndpoint();

        return functionApp;
    }

    private static string CreateDefaultStorageName(IDistributedApplicationBuilder builder)
    {
        // Use ProjectNameSha256 for stable naming across deployments
        var applicationHash = builder.Configuration["AppHost:ProjectNameSha256"]?[..5].ToLowerInvariant() ?? "local";
        return $"funcstorage{applicationHash}";
    }

    /// <summary>
    /// Configures the Azure Functions project resource to use the specified port as its HTTP endpoint.
    /// This method queries the launch profile of the project to determine the port to use based on 
    /// the command line arguments configured in the launch profile.
    /// </summary>
    /// <param name="builder">The resource builder for the Azure Functions project resource.</param>
    /// <returns>The resource builder with HTTP endpoint configured.</returns>
    private static IResourceBuilder<ProjectResource> WithFunctionsHttpEndpoint(this IResourceBuilder<ProjectResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder
                .WithHttpEndpoint(targetPort: 8080)
                .WithHttpsEndpoint(targetPort: 8080);
        }

        return builder
            .WithHttpEndpoint(port: null, targetPort: null, isProxied: true)
            .WithArgs(context =>
            {
                // If we're running in publish mode, we don't need to map the port the host should listen on.
                if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
                {
                    return;
                }
                var http = builder.Resource.GetEndpoint("http");
                context.Args.Add("--port");
                context.Args.Add(http.Property(EndpointProperty.TargetPort));
            });
    }
}
