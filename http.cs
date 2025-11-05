#:package Microsoft.ApplicationInsights.WorkerService@2.23.0
#:package Microsoft.Azure.Functions.Worker@2.50.0-preview2
#:package Microsoft.Azure.Functions.Worker.ApplicationInsights@2.0.0
#:package Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore@2.1.0
#:package Microsoft.Azure.Functions.Worker.Sdk@2.0.5
#:property PublishAoT=false

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Build().Run();

public class HttpTriggerImpl(ILogger<HttpTriggerImpl> logger)
{
    [Function(nameof(HttpTriggerImpl))]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}