using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;

public class VideoAnalysisAgent
{
    private readonly ChatClient _chatClient;
    private readonly ILogger<VideoAnalysisAgent> _logger;
    private readonly AIAgent _agent;

    public VideoAnalysisAgent(
        ChatClient chatClient,
        ILogger<VideoAnalysisAgent> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
        
        // Create the AIAgent with instructions
        _agent = _chatClient.CreateAIAgent(
            instructions: "You are a video content analyzer. Analyze videos and extract key topics, themes, and provide comprehensive summaries.",
            name: "VideoAnalyzer");
    }

    public async Task<VideoAnalysisResult> AnalyzeVideoAsync(
        string videoPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing video: {VideoPath}", videoPath);

        // Use the RunAsync method on AIAgent
        var response = await _agent.RunAsync(
            $"Analyze this video and extract key topics: {videoPath}",
            cancellationToken: cancellationToken);

        // Extract the text from the response messages
        var summaryText = string.Join("\n", 
            response.Messages
                .SelectMany(m => m.Contents)
                .OfType<Microsoft.Extensions.AI.TextContent>()
                .Select(t => t.Text));

        return new VideoAnalysisResult
        {
            Summary = summaryText,
            Topics = ExtractTopics(summaryText)
        };
    }

    private List<string> ExtractTopics(string text)
    {
        // Topic extraction logic
        return new List<string>();
    }
}

public record VideoAnalysisResult
{
    public string Summary { get; init; } = string.Empty;
    public List<string> Topics { get; init; } = new();
}