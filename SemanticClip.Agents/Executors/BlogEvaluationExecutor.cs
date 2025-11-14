using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

namespace SemanticClip.Agents.Executors;

/// <summary>
/// Executor that evaluates and improves a blog post using Azure AI Foundry.
/// Uses OpenAI client with Microsoft Agent Framework to enhance content quality,
/// relevance, engagement, and SEO.
/// </summary>
public class BlogEvaluationExecutor : ReflectingExecutor<BlogEvaluationExecutor>, IMessageHandler<string, string>
{
    private readonly ILogger<BlogEvaluationExecutor> _logger;
    private readonly IConfiguration _configuration;
    private readonly OpenAIClient? _openAIClient;

    public BlogEvaluationExecutor(
        ILogger<BlogEvaluationExecutor> logger,
        IConfiguration configuration) : base("BlogEvaluation")
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
    /// Evaluates and improves a blog post using AI.
    /// </summary>
    /// <param name="blogPost">The original blog post to improve</param>
    /// <returns>Improved blog post content</returns>
    public async ValueTask<string> HandleAsync(string blogPost, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            throw new InvalidOperationException(
                "Azure AI Foundry client is not configured. Please check your appsettings.json");
        }

        _logger.LogDebug("Starting blog post evaluation and improvement");
        Console.WriteLine($"\n🔍 Evaluating and improving blog post...");

        try
        {
            var deploymentName = _configuration["AzureAIFoundry:ChatDeploymentName"] ?? "gpt-4o";
            
            // Get the chat client using the deployment name
            var chatClient = _openAIClient.GetChatClient(deploymentName);

            // Evaluation prompt based on the YAML template
            var systemPrompt = @"You are a professional technical writer and content editor. Based on the following blog post, generate an improved version that addresses these key aspects:

1. **Content Quality**: Improve structure, clarity, grammar, and technical accuracy
2. **Relevance**: Better address the topic and target audience  
3. **Engagement**: Enhance writing style, readability, and reader engagement
4. **SEO**: Optimize content with relevant keywords while maintaining natural flow
5. **Professional Formatting**: Create a polished, publication-ready blog post

**CRITICAL Markdown Formatting Rules:**
- Use a single # H1 heading for the main title only
- Use ## H2 for all major sections
- Use ### H3 for subsections only when needed
- Use - or * for bullet lists consistently
- Add blank lines between sections for readability
- Use **bold** for emphasis and `code` for technical terms
- Include a brief introduction paragraph after the title
- Add a conclusion section at the end
- Use horizontal rules (---) sparingly to separate major sections

**Output Requirements:**
- The output must be a complete, ready-to-publish blog post in markdown format
- Do NOT include any explanations, comments, or meta-text
- Do NOT use ### for main sections - only use ## H2
- Start directly with the # title
- Maintain the same core message while significantly enhancing quality and professionalism";

            var userPrompt = $"Original Blog Post:\n\n{blogPost}\n\nProvide the improved, professionally formatted blog post:";

            _logger.LogDebug("Calling Azure AI Foundry to evaluate and improve blog post...");
            Console.WriteLine($"⏳ Calling Azure AI to improve content quality, SEO, and engagement...");

            // Create chat messages
            var messages = new ChatMessage[]
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt)
            };

            // Call the chat completion API
            var completion = await chatClient.CompleteChatAsync(messages);
            var improvedBlogPost = completion.Value.Content[0].Text;

            if (string.IsNullOrWhiteSpace(improvedBlogPost))
            {
                throw new InvalidOperationException("Azure AI returned an empty response");
            }

            Console.WriteLine($"✅ Blog post evaluated and improved: {improvedBlogPost.Length} characters");

            return improvedBlogPost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating blog post");
            Console.WriteLine($"❌ Evaluation failed: {ex.Message}");
            throw;
        }
    }
}
