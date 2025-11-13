using Microsoft.Extensions.Logging;
using SemanticClip.Agents.Executors;
using SemanticClip.Agents.Models;

namespace SemanticClip.Agents.Workflows;

/// <summary>
/// Main workflow orchestrator for video processing using Microsoft Agent Framework patterns.
/// This workflow coordinates the execution of multiple steps:
/// 1. Get video file path from user (FileInputExecutor)
/// 2. Extract audio using FFmpeg (AudioExtractionExecutor)
/// 3. Transcribe audio (TranscriptionExecutor)
/// 4. Generate blog post (BlogGenerationExecutor)
/// 5. Evaluate and improve blog post (BlogEvaluationExecutor)
/// 6. Publish blog post to GitHub (BlogPublishingExecutor)
/// </summary>
public class VideoProcessingWorkflow
{
    private readonly FileInputExecutor _fileInputExecutor;
    private readonly AudioExtractionExecutor _audioExtractionExecutor;
    private readonly TranscriptionExecutor _transcriptionExecutor;
    private readonly BlogGenerationExecutor _blogGenerationExecutor;
    private readonly BlogEvaluationExecutor _blogEvaluationExecutor;
    private readonly BlogPublishingExecutor _blogPublishingExecutor;
    private readonly ILogger<VideoProcessingWorkflow> _logger;

    public VideoProcessingWorkflow(
        FileInputExecutor fileInputExecutor,
        AudioExtractionExecutor audioExtractionExecutor,
        TranscriptionExecutor transcriptionExecutor,
        BlogGenerationExecutor blogGenerationExecutor,
        BlogEvaluationExecutor blogEvaluationExecutor,
        BlogPublishingExecutor blogPublishingExecutor,
        ILogger<VideoProcessingWorkflow> logger)
    {
        _fileInputExecutor = fileInputExecutor;
        _audioExtractionExecutor = audioExtractionExecutor;
        _transcriptionExecutor = transcriptionExecutor;
        _blogGenerationExecutor = blogGenerationExecutor;
        _blogEvaluationExecutor = blogEvaluationExecutor;
        _blogPublishingExecutor = blogPublishingExecutor;
        _logger = logger;
    }

    /// <summary>
    /// Executes the complete video processing workflow.
    /// </summary>
    public async Task<VideoProcessingResult> RunAsync()
    {
        _logger.LogDebug("Starting Video Processing Workflow");
        Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║        SemanticClip - Agent Framework Video Processor       ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝\n");

        string? videoPath = null;
        string? audioPath = null;
        string? transcript = null;
        string? blogPost = null;
        string? evaluatedBlogPost = null;

        try
        {
            // Step 1: Get video file from user
            Console.WriteLine("📋 Step 1: File Selection");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            videoPath = await _fileInputExecutor.GetVideoFilePathAsync();
            
            // Step 2: Extract audio from video
            Console.WriteLine("\n📋 Step 2: Audio Extraction");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            audioPath = await _audioExtractionExecutor.ExtractAudioAsync(videoPath);

            // Step 3: Transcribe audio using Azure OpenAI Whisper
            Console.WriteLine("\n📋 Step 3: Audio Transcription");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            
            if (!_transcriptionExecutor.IsConfigured())
            {
                Console.WriteLine("⚠️  Azure OpenAI is not configured. Skipping transcription.");
                Console.WriteLine("   Please configure AzureOpenAI:Endpoint and AzureOpenAI:ApiKey in appsettings.json");
            }
            else
            {
                transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
            }

            // Step 4: Generate blog post from transcript
            Console.WriteLine("\n📋 Step 4: Blog Post Generation");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            
            if (string.IsNullOrWhiteSpace(transcript))
            {
                Console.WriteLine("⚠️  No transcript available. Skipping blog generation.");
            }
            else if (!_blogGenerationExecutor.IsConfigured())
            {
                Console.WriteLine("⚠️  Azure OpenAI is not configured. Skipping blog generation.");
            }
            else
            {
                blogPost = await _blogGenerationExecutor.GenerateBlogPostAsync(transcript, videoPath);
            }

            // Step 5: Evaluate and improve the blog post
            Console.WriteLine("\n📋 Step 5: Blog Post Evaluation");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            
            if (string.IsNullOrWhiteSpace(blogPost))
            {
                Console.WriteLine("⚠️  No blog post available. Skipping evaluation.");
            }
            else if (!_blogEvaluationExecutor.IsConfigured())
            {
                Console.WriteLine("⚠️  Azure AI Foundry is not configured. Skipping evaluation.");
                evaluatedBlogPost = blogPost; // Use original if evaluation is skipped
            }
            else
            {
                evaluatedBlogPost = await _blogEvaluationExecutor.EvaluateBlogPostAsync(blogPost, videoPath);
            }

            // Step 6: Publish blog post to GitHub
            Console.WriteLine("\n📋 Step 6: Blog Publishing");
            Console.WriteLine("──────────────────────────────────────────────────────────────");
            
            BlogPublishingResponse? publishResponse = null;
            var blogToPublish = evaluatedBlogPost ?? blogPost; // Use evaluated version if available
            
            if (string.IsNullOrWhiteSpace(blogToPublish))
            {
                Console.WriteLine("⚠️  No blog post available. Skipping publishing.");
            }
            else if (!_blogPublishingExecutor.IsConfigured())
            {
                Console.WriteLine("⚠️  Azure AI Foundry or GitHub is not configured. Skipping publishing.");
                Console.WriteLine("   Please configure GitHub:Repo and GitHub:PersonalAccessToken in appsettings.json");
            }
            else
            {
                publishResponse = await _blogPublishingExecutor.PublishBlogPostAsync(blogToPublish, videoPath);
            }

            _logger.LogDebug("Workflow completed successfully");
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    ✅ Workflow Complete!                     ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");

            var message = publishResponse?.Success == true
                ? $"Blog post published to GitHub successfully!"
                : evaluatedBlogPost != null
                    ? $"Blog post evaluated and improved successfully!"
                    : blogPost != null
                        ? $"Blog post generated successfully!"
                        : transcript != null
                            ? $"Transcription completed. {transcript.Length} characters transcribed."
                            : "Audio extraction completed.";

            // Display summary
            Console.WriteLine("\n📊 Summary:");
            Console.WriteLine($"   Video File: {videoPath}");
            Console.WriteLine($"   Status: {message}");
            
            if (publishResponse?.Success == true && !string.IsNullOrEmpty(publishResponse.Result))
            {
                Console.WriteLine($"   🔗 Published URL: {publishResponse.Result}");
            }

            return new VideoProcessingResult
            {
                Success = true,
                VideoPath = videoPath,
                AudioPath = audioPath,
                Transcript = transcript,
                BlogPost = blogPost,
                EvaluatedBlogPost = evaluatedBlogPost,
                PublishingResult = publishResponse,
                Message = message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Workflow failed with error");
            Console.WriteLine($"\n❌ Error: {ex.Message}");

            return new VideoProcessingResult
            {
                Success = false,
                VideoPath = videoPath,
                AudioPath = audioPath,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            // Cleanup: Delete temporary audio file
            if (!string.IsNullOrEmpty(audioPath))
            {
                Console.WriteLine("\n🧹 Cleaning up temporary files...");
                _audioExtractionExecutor.CleanupAudioFile(audioPath);
            }
        }
    }
}

/// <summary>
/// Result of the video processing workflow execution.
/// </summary>
public class VideoProcessingResult
{
    public bool Success { get; set; }
    public string? VideoPath { get; set; }
    public string? AudioPath { get; set; }
    public string? Transcript { get; set; }
    public string? BlogPost { get; set; }
    public string? EvaluatedBlogPost { get; set; }
    public BlogPublishingResponse? PublishingResult { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
