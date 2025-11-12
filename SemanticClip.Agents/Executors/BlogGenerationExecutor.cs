using System;
using System.ClientModel.Primitives;
using System.IO;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace SemanticClip.Agents.Executors;

/// <summary>
/// Executor that generates a blog post from a transcript using Azure AI Foundry.
/// Uses OpenAI client with Microsoft Agent Framework.
/// </summary>
public class BlogGenerationExecutor
{
    private readonly ILogger<BlogGenerationExecutor> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient? _openAIClient;

    public BlogGenerationExecutor(
        ILogger<BlogGenerationExecutor> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        // Initialize OpenAI client for Azure AI Foundry
        var endpoint = _configuration["AzureAIFoundry:Endpoint"];
        var apiKey = _configuration["AzureAIFoundry:ApiKey"];

        if (!string.IsNullOrEmpty(endpoint))
        {
            // Azure AI Foundry endpoint needs /openai/v1/ appended
            var foundryEndpoint = endpoint.TrimEnd('/') + "/openai/v1/";
            
            var clientOptions = new OpenAIClientOptions() 
            { 
                Endpoint = new Uri(foundryEndpoint) 
            };

            if (!string.IsNullOrEmpty(apiKey))
            {
                // Use API key authentication
                _openAIClient = new OpenAIClient(new System.ClientModel.ApiKeyCredential(apiKey), clientOptions);
            }
            else
            {
                // Use Azure credential as shown in the documentation
#pragma warning disable OPENAI001
                _openAIClient = new OpenAIClient(
                    new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"), 
                    clientOptions);
#pragma warning restore OPENAI001
            }
        }
    }

    /// <summary>
    /// Generates a blog post from the transcript using Azure OpenAI.
    /// </summary>
    /// <param name="transcript">The video transcript</param>
    /// <param name="videoFilePath">Original video file path (for saving markdown in same directory)</param>
    /// <returns>Generated blog post in markdown format</returns>
    public async Task<string> GenerateBlogPostAsync(string transcript, string videoFilePath)
    {
        if (_openAIClient == null)
        {
            throw new InvalidOperationException(
                "Azure OpenAI client is not configured. Please check your appsettings.json");
        }

        _logger.LogDebug("Starting blog post generation from transcript");
        Console.WriteLine($"\n✍️  Generating blog post from transcript...");

        try
        {
            var deploymentName = _configuration["AzureAIFoundry:ChatDeploymentName"] ?? "gpt-4o";
            
            // Get the chat client using the deployment name
            var chatClient = _openAIClient.GetChatClient(deploymentName);
            
            // Create an AIAgent using the Agent Framework extension
            var agent = chatClient.CreateAIAgent(
                instructions: @"You are an expert blog post writer. Generate a well-structured, engaging blog post in markdown format from video transcripts. 

Guidelines:
- Create a compelling title (# header)
- Include an introduction that hooks the reader
- Break down content into logical sections with subheadings (## headers)
- Use bullet points or numbered lists where appropriate
- Add relevant code blocks if discussing technical content
- Include a conclusion that summarizes key points
- Ensure all markdown is valid and well-formatted
- Make the content informative and engaging",
                name: "BlogWriter");

            Console.WriteLine("⏳ Calling Azure OpenAI to generate blog post...");

            // Run the agent with the transcript
            var response = await agent.RunAsync($@"Generate a blog post from the following video transcript. Format it in markdown with proper structure:

{transcript}");

            // Extract the text from the response
            var blogPost = response.Text;

            if (string.IsNullOrWhiteSpace(blogPost))
            {
                throw new InvalidOperationException("Blog post generation returned empty result");
            }

            _logger.LogDebug("Blog post generated successfully. Length: {Length} characters", blogPost.Length);
            Console.WriteLine($"✅ Blog post generated: {blogPost.Length} characters");

            // Save the markdown file
            await SaveMarkdownFileAsync(blogPost, videoFilePath);

            return blogPost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during blog post generation");
            Console.WriteLine($"❌ Blog generation failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Saves the generated blog post as a markdown file in the same directory as the video.
    /// </summary>
    private async Task SaveMarkdownFileAsync(string blogPost, string videoFilePath)
    {
        try
        {
            var videoDirectory = Path.GetDirectoryName(videoFilePath) ?? Directory.GetCurrentDirectory();
            var videoFileName = Path.GetFileNameWithoutExtension(videoFilePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var markdownFileName = $"{videoFileName}-Generated-{timestamp}.md";
            var markdownFilePath = Path.Combine(videoDirectory, markdownFileName);

            await File.WriteAllTextAsync(markdownFilePath, blogPost);

            _logger.LogDebug("Markdown file saved: {FilePath}", markdownFilePath);
            Console.WriteLine($"\n💾 Blog post saved to:");
            Console.WriteLine($"   {markdownFilePath}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save markdown file");
            Console.WriteLine($"⚠️  Warning: Could not save markdown file: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates that Azure OpenAI is configured
    /// </summary>
    public bool IsConfigured()
    {
        return _openAIClient != null;
    }
}
