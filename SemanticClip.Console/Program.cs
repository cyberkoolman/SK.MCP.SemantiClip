using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticClip.Core.Interfaces;
using SemanticClip.Core.Models;
using SemanticClip.Services;
using SemanticClip.Services.Steps;
using System.Text;
using static SemanticClip.Services.Steps.GenerateBlogPostStep;

var builder = Host.CreateApplicationBuilder(args);

// First on Logging
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();

// Register SK Kernel ON THE SAME IServiceCollection
var kb = builder.Services.AddKernel();

kb.AddAzureOpenAIChatCompletion(
    builder.Configuration["AzureOpenAI:ContentDeploymentName"]!,
    builder.Configuration["AzureOpenAI:Endpoint"]!,
    builder.Configuration["AzureOpenAI:ApiKey"]!);

kb.AddAzureOpenAIAudioToText(
    builder.Configuration["AzureOpenAI:WhisperDeploymentName"]!,
    builder.Configuration["AzureOpenAI:Endpoint"]!,
    builder.Configuration["AzureOpenAI:ApiKey"]!);

// Register all the Semantic Kernel process steps (required by VideoProcessingService)
builder.Services.AddTransient<PrepareVideoStep>();
builder.Services.AddTransient<TranscribeVideoStep>();
builder.Services.AddTransient<GenerateBlogPostStep>();
builder.Services.AddTransient<EvaluateBlogPostStep>();
builder.Services.AddTransient<PublishBlogPostStep>();

// Register the existing SemanticClip services
builder.Services.AddScoped<IVideoProcessingService, VideoProcessingService>();
builder.Services.AddScoped<IBlogPublishingService, BlogPublishingService>();

var host = builder.Build();

// get a logger for the console app itself
var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Service
var videoProcessingService = host.Services.GetRequiredService<IVideoProcessingService>();
var blogPublishingService = host.Services.GetRequiredService<IBlogPublishingService>();

try
{
    // Get video file path from user
    Console.Write("Enter video file path: ");
    var filePath = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
    {
        logger.LogWarning("Invalid file path.");
        return;
    }

    logger.LogInformation($"Processing {Path.GetFileName(filePath)}...");

    // Read file and create request
    var fileBytes = await File.ReadAllBytesAsync(filePath);
    var request = new VideoProcessingRequest
    {
        FileName = Path.GetFileName(filePath),
        FileContent = Convert.ToBase64String(fileBytes),
        Title = Path.GetFileNameWithoutExtension(filePath),
        Description = Path.GetDirectoryName(filePath)
    };

    // Before calling ProcessVideoAsync, store the file path
    FilePathStorage.CurrentFilePath = filePath;

    // Process video
    var response = await videoProcessingService.ProcessVideoAsync(request);

    if (string.IsNullOrEmpty(response.ErrorMessage) && response.Status != "Failed")
    {
        Console.WriteLine("Processing completed successfully!");
        Console.WriteLine("Markdown file was saved during blog post generation.");

        // Ask if user wants to publish the blog
        Console.WriteLine();
        Console.Write("Would you like to publish the blog? (y/n): ");
        var input = Console.ReadLine();
        
        if (input?.Trim().ToLower() == "y")
        {
            try
            {
                logger.LogInformation("Publishing blog post...");

                var publishRequest = new BlogPostPublishRequest
                {
                    BlogPost = response.BlogPost,
                    CommitMessage = $"Auto-publish blog post: {request.Title} - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                };
                
                var publishResponse = await blogPublishingService.PublishBlogPostAsync(publishRequest);

                if (publishResponse.Success)
                {
                    logger.LogInformation("✅ Blog published successfully! {Url}", publishResponse.Result);
                }
                else
                {
                    logger.LogError("❌ Blog publishing failed: {Message}", publishResponse.Message);
                }
            }
            catch (Exception publishEx)
            {
                logger.LogError("❌ Error during blog publishing: {Message}", publishEx.Message);
            }
        }
        else
        {
            logger.LogInformation("Blog was not published.");
        }
    }
    else
    {
        logger.LogError($"Processing failed: {response.ErrorMessage}");
        logger.LogInformation("Check if a partial markdown file was saved during processing.");
    }
}
catch (Exception ex)
{
    logger.LogError($"Error: {ex.Message}");
}