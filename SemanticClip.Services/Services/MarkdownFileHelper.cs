using System.Text;
using Microsoft.Extensions.Logging;
using SemanticClip.Core.Models;

namespace SemanticClip.Services;

public static class MarkdownFileHelper
{
    public static async Task SaveMarkdownFileAsync(
        VideoProcessingResponse response,
        string? filePath,
        string stage,                  // <-- Add this
        ILogger? logger = null)
    {
        try
        {
            var originalFilePath = filePath ?? "video.mp4";
            var directory = Path.GetDirectoryName(originalFilePath) ?? Environment.CurrentDirectory;
            var fileName = Path.GetFileName(originalFilePath);

            // Add stage and timestamp for uniqueness
            var outputFileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{stage}_{DateTime.Now:yyyyMMdd_HHmmss}.md";
            var outputPath = Path.Combine(directory, outputFileName);

            var markdownContent = GenerateMarkdownContent(response);
            await File.WriteAllTextAsync(outputPath, markdownContent, Encoding.UTF8);

            logger?.LogInformation("Markdown file saved to: {OutputPath}", outputPath);
            Console.WriteLine($"Markdown file saved to: {outputPath}");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to save markdown file");
        }
    }


    public static string GenerateMarkdownContent(VideoProcessingResponse response)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Video Processing Results");
        sb.AppendLine();
        sb.AppendLine($"**Processed:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(response.BlogPost))
        {
            sb.AppendLine("## Blog Post");
            sb.AppendLine();
            sb.AppendLine(response.BlogPost);
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(response.Transcript))
        {
            sb.AppendLine("## Transcript");
            sb.AppendLine();
            sb.AppendLine("```");
            sb.AppendLine(response.Transcript);
            sb.AppendLine("```");
        }

        return sb.ToString();
    }
}
