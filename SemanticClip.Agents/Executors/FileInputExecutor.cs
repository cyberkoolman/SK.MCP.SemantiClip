using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Agents.Executors;

/// <summary>
/// Executor that handles user input for video file path and validates the file exists.
/// This is the entry point of the video processing workflow.
/// </summary>
public class FileInputExecutor : ReflectingExecutor<FileInputExecutor>, IMessageHandler<string, string>
{
    private readonly ILogger<FileInputExecutor> _logger;

    public FileInputExecutor(ILogger<FileInputExecutor> logger) : base("FileInput")
    {
        _logger = logger;
    }

    /// <summary>
    /// Prompts the user for a video file path and validates it exists.
    /// Returns the validated file path for the next executor in the workflow.
    /// </summary>
    public ValueTask<string> HandleAsync(string input, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        while (true)
        {
            Console.Write("Enter the path to your MP4 video file: ");
            var filePath = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                Console.WriteLine("❌ File path cannot be empty. Please try again.");
                continue;
            }

            // Remove quotes if user pasted a path with quotes
            filePath = filePath.Trim('"', '\'');

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"❌ File not found: {filePath}");
                Console.WriteLine("Please check the path and try again.");
                continue;
            }

            // Validate it's a video file (basic check)
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (extension != ".mp4" && extension != ".avi" && extension != ".mov" && extension != ".mkv")
            {
                Console.WriteLine($"⚠️  Warning: File extension '{extension}' is not a common video format.");
                Console.Write("Continue anyway? (y/n): ");
                var response = Console.ReadLine()?.Trim().ToLowerInvariant();
                
                if (response != "y")
                {
                    continue;
                }
            }

            _logger.LogDebug("Valid video file selected: {FilePath}", filePath);
            Console.WriteLine($"✅ File validated: {Path.GetFileName(filePath)} ({GetFileSize(filePath)})");

            return ValueTask.FromResult(filePath);
        }
    }

    private static string GetFileSize(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var bytes = fileInfo.Length;

        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
