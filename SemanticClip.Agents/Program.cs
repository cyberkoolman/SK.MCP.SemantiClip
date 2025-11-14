using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SemanticClip.Agents.Executors;
using SemanticClip.Agents.Models;
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

var host = builder.Build();

// Display banner
Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        SemanticClip - Agent Framework Video Processor       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

try
{
    // Get executors from DI container
    var fileInputExecutor = host.Services.GetRequiredService<FileInputExecutor>();
    var audioExtractionExecutor = host.Services.GetRequiredService<AudioExtractionExecutor>();
    var transcriptionExecutor = host.Services.GetRequiredService<TranscriptionExecutor>();
    var blogGenerationExecutor = host.Services.GetRequiredService<BlogGenerationExecutor>();
    var blogEvaluationExecutor = host.Services.GetRequiredService<BlogEvaluationExecutor>();
    var blogPublishingExecutor = host.Services.GetRequiredService<BlogPublishingExecutor>();

    // Create the workflow using WorkflowBuilder pattern
    var workflow = VideoProcessingWorkflow.Create(
        fileInputExecutor,
        audioExtractionExecutor,
        transcriptionExecutor,
        blogGenerationExecutor,
        blogEvaluationExecutor,
        blogPublishingExecutor);

    Console.WriteLine("🚀 Starting workflow execution...\n");

    // Execute the workflow using the Agent Framework InProcessExecution
    var run = await InProcessExecution.RunAsync(workflow, "start");

    BlogPublishingResponse? finalResult = null;
    string? lastExecutorId = null;

    // Process workflow events
    foreach (WorkflowEvent evt in run.NewEvents)
    {
        if (evt is ExecutorCompletedEvent completed)
        {
            lastExecutorId = completed.ExecutorId;
            Console.WriteLine($"✅ Completed: {completed.ExecutorId}");
        }
        else if (evt is WorkflowOutputEvent output)
        {
            Console.WriteLine($"📤 Workflow Output Event received");
            
            // The final output is the BlogPublishingResponse
            if (output.Data is BlogPublishingResponse response)
            {
                finalResult = response;
            }
        }
    }

    // Display final results
    Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                    ✅ Workflow Complete!                     ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

    if (finalResult != null)
    {
        Console.WriteLine($"\n📊 Summary:");
        Console.WriteLine($"   Status: {(finalResult.Success ? "Success" : "Failed")}");
        Console.WriteLine($"   Message: {finalResult.Message}");
        
        if (finalResult.Success && !string.IsNullOrEmpty(finalResult.Result))
        {
            Console.WriteLine($"   🔗 Published URL: {finalResult.Result}");
        }
    }

    return finalResult?.Success == true ? 0 : 1;
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Error: {ex.Message}");
    Console.WriteLine($"   {ex.StackTrace}");
    return 1;
}
