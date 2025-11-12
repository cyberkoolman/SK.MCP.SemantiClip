using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

public class BlogGenerationAgent
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<BlogGenerationAgent> _logger;
    private readonly AIAgent _agent;

    public BlogGenerationAgent(
        ChatClient chatClient,
        ILogger<BlogGenerationAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
        
        // Create the AIAgent with instructions
        _agent = _chatClient.CreateAIAgent(
            instructions: "You are a professional blog writer. Generate engaging, well-structured blog posts from video analysis data. Create compelling titles, clear sections, and engaging content.",
            name: "BlogWriter");
    }

    public async Task<BlogPost> GenerateBlogPostAsync(
        VideoAnalysisResult analysis,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating blog post from video analysis");

        var prompt = $@"Create a blog post based on the following video analysis:
Summary: {analysis.Summary}
Topics: {string.Join(", ", analysis.Topics)}

Please generate a well-structured blog post with:
- An engaging title
- An introduction
- Main content sections
- A conclusion";

        // Use the RunAsync method on AIAgent
        var response = await _agent.RunAsync(
            prompt,
            cancellationToken: cancellationToken);

        // Extract the text from the response messages
        var blogContent = string.Join("\n", 
            response.Messages
                .SelectMany(m => m.Contents)
                .OfType<Microsoft.Extensions.AI.TextContent>()
                .Select(t => t.Text));

        return new BlogPost
        {
            Title = ExtractTitle(blogContent),
            Content = blogContent,
            Topics = analysis.Topics
        };
    }

    private string ExtractTitle(string content)
    {
        // Simple title extraction logic - could be enhanced
        var lines = content.Split('\n');
        return lines.FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim() ?? "Untitled Blog Post";
    }
}

public record BlogPost
{
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public List<string> Topics { get; init; } = new();
}
