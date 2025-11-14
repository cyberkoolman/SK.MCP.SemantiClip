using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.Logging;
using SemanticClip.Agents.Executors;
using SemanticClip.Agents.Models;

namespace SemanticClip.Agents.Workflows;

/// <summary>
/// Factory for creating video processing workflow using Microsoft Agent Framework WorkflowBuilder.
/// This workflow coordinates the execution of multiple steps:
/// 1. Get video file path from user (FileInputExecutor)
/// 2. Extract audio using FFmpeg (AudioExtractionExecutor)
/// 3. Transcribe audio (TranscriptionExecutor)
/// 4. Generate blog post (BlogGenerationExecutor)
/// 5. Evaluate and improve blog post (BlogEvaluationExecutor)
/// 6. Publish blog post to GitHub (BlogPublishingExecutor)
/// </summary>
public static class VideoProcessingWorkflow
{
    /// <summary>
    /// Creates a workflow using the Microsoft Agent Framework WorkflowBuilder pattern.
    /// </summary>
    public static Workflow Create(
        FileInputExecutor fileInputExecutor,
        AudioExtractionExecutor audioExtractionExecutor,
        TranscriptionExecutor transcriptionExecutor,
        BlogGenerationExecutor blogGenerationExecutor,
        BlogEvaluationExecutor blogEvaluationExecutor,
        BlogPublishingExecutor blogPublishingExecutor)
    {
        // Build the workflow graph using WorkflowBuilder
        var workflow = new WorkflowBuilder(fileInputExecutor)
            .AddEdge(fileInputExecutor, audioExtractionExecutor)
            .AddEdge(audioExtractionExecutor, transcriptionExecutor)
            .AddEdge(transcriptionExecutor, blogGenerationExecutor)
            .AddEdge(blogGenerationExecutor, blogEvaluationExecutor)
            .AddEdge(blogEvaluationExecutor, blogPublishingExecutor)
            .WithOutputFrom(blogPublishingExecutor)
            .Build();

        return workflow;
    }
}
