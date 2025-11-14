using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Transport;
using OpenAI;
using OpenAI.Chat;
using SemanticClip.Agents.Models;

namespace SemanticClip.Agents.Executors;

/// <summary>
/// Executor that publishes a blog post to GitHub using MCP (Model Context Protocol).
/// Uses Microsoft Agent Framework with MCP client integration.
/// </summary>
public class BlogPublishingExecutor : ReflectingExecutor<BlogPublishingExecutor>, IMessageHandler<string, BlogPublishingResponse>
{
    private readonly ILogger<BlogPublishingExecutor> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient? _openAIClient;

    public BlogPublishingExecutor(
        ILogger<BlogPublishingExecutor> logger,
        IConfiguration configuration) : base("BlogPublishing")
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
                // Use Azure credential
                _openAIClient = new OpenAIClient(
                    new System.ClientModel.ApiKeyCredential("dummy-key"), 
                    clientOptions);
            }
        }
    }

    /// <summary>
    /// Checks if the executor is properly configured.
    /// </summary>
    public bool IsConfigured()
    {
        return _openAIClient != null;
    }

    /// <summary>
    /// Publishes a blog post to GitHub using MCP client.
    /// </summary>
    /// <param name="blogPost">The blog post content to publish</param>
    /// <returns>Publishing result details</returns>
    public async ValueTask<BlogPublishingResponse> HandleAsync(string blogPost, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            throw new InvalidOperationException("Azure AI Foundry client is not configured");
        }

        try
        {
            Console.WriteLine("\n📤 Publishing blog post to GitHub...");

            // Get GitHub configuration
            var githubRepo = _configuration["GitHub:Repo"];
            var githubToken = _configuration["GitHub:PersonalAccessToken"];

            if (string.IsNullOrEmpty(githubRepo))
            {
                throw new InvalidOperationException("GitHub:Repo not configured in appsettings.json");
            }

            if (string.IsNullOrEmpty(githubToken))
            {
                throw new InvalidOperationException("GitHub:PersonalAccessToken not configured in appsettings.json");
            }

            // Create MCP client for GitHub
            var mcpClient = await GetMcpClientAsync(githubToken);
            var mcpTools = await mcpClient.ListToolsAsync();

            _logger.LogInformation("Retrieved {ToolCount} MCP tools from GitHub server", mcpTools.Count);
            Console.WriteLine($"🔧 Retrieved {mcpTools.Count} MCP tools from GitHub server");

            // Generate filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var filename = $"blog-posts/generated-post-{timestamp}.md";
            var commitMessage = $"Add blog post: {timestamp}";

            Console.WriteLine($"📝 Target file: {filename}");
            Console.WriteLine($"📦 Repository: {githubRepo}");
            Console.WriteLine($"⏳ Publishing to GitHub using MCP tools...");

            // Use the create_or_update_file tool directly
            var createFileTool = mcpTools.FirstOrDefault(t => t.Name == "create_or_update_file");
            if (createFileTool == null)
            {
                throw new InvalidOperationException("create_or_update_file tool not found in MCP tools");
            }

            // Call the MCP tool to create the file
            var callResult = await mcpClient.CallToolAsync(
                createFileTool.Name,
                new Dictionary<string, object?>
                {
                    { "owner", githubRepo.Split('/')[0] },
                    { "repo", githubRepo.Split('/')[1] },
                    { "path", filename },
                    { "content", blogPost },
                    { "message", commitMessage },
                    { "branch", "main" }
                });

            var result = callResult.Content?.FirstOrDefault()?.Text ?? "File created successfully";
            
            // Construct the GitHub URL
            var githubUrl = $"https://github.com/{githubRepo}/blob/main/{filename}";
            
            Console.WriteLine($"✅ Blog post published to GitHub!");
            Console.WriteLine($"   📂 File: {filename}");
            Console.WriteLine($"   📄 Size: {blogPost.Length} characters");
            Console.WriteLine($"   🔗 URL: {githubUrl}");

            return new BlogPublishingResponse
            {
                Success = true,
                Message = "Blog post published successfully to GitHub",
                Result = githubUrl
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing blog post to GitHub");
            Console.WriteLine($"❌ Publishing failed: {ex.Message}");

            return new BlogPublishingResponse
            {
                Success = false,
                Message = $"Error publishing blog post: {ex.Message}",
                Result = null
            };
        }
    }

    /// <summary>
    /// Creates an MCP client for GitHub operations.
    /// </summary>
    private async Task<IMcpClient> GetMcpClientAsync(string githubToken)
    {
        // Create an MCPClient for the GitHub server
        var mcpClient = await McpClientFactory.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "GitHub",
            Command = "npx",
            Arguments = ["-y", "@modelcontextprotocol/server-github"],
            EnvironmentVariables = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { "GITHUB_PERSONAL_ACCESS_TOKEN", githubToken }
            }
        }));

        return mcpClient;
    }
}
