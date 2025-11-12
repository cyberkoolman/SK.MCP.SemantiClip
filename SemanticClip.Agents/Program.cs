using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticClip.Agents.Executors;
using SemanticClip.Agents.Workflows;

// Build the host with dependency injection and logging
var builder = Host.CreateApplicationBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Warning); // Only show warnings and errors by default

// Register executors
builder.Services.AddTransient<FileInputExecutor>();
builder.Services.AddTransient<AudioExtractionExecutor>();
builder.Services.AddTransient<TranscriptionExecutor>();
builder.Services.AddTransient<BlogGenerationExecutor>();
builder.Services.AddTransient<BlogEvaluationExecutor>();
builder.Services.AddTransient<BlogPublishingExecutor>();

// Register workflow orchestrator
builder.Services.AddTransient<VideoProcessingWorkflow>();

var host = builder.Build();

// Get the workflow from DI container
var workflow = host.Services.GetRequiredService<VideoProcessingWorkflow>();

// Execute the workflow
var result = await workflow.RunAsync();

// Display final results
if (result.Success)
{
    // Summary already displayed in workflow
}
else
{
    Console.WriteLine($"\n❌ Workflow failed: {result.ErrorMessage}");
    return 1;
}

return 0;
