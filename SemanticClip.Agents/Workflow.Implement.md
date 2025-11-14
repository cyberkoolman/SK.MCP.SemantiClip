# Microsoft Agent Framework Workflow Implementation

## Overview

This document captures the transformation from a custom executor pattern to the official **Microsoft Agent Framework Workflow** implementation.

## What It Was: Custom Pattern (Before)

### Custom Executor Pattern

**Executors were plain C# classes:**

```csharp
public class FileInputExecutor
{
    private readonly ILogger<FileInputExecutor> _logger;

    public FileInputExecutor(ILogger<FileInputExecutor> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetVideoFilePathAsync()
    {
        // Custom implementation
        return filePath;
    }
}
```

**Key Characteristics:**
- Plain classes with no framework base
- Custom method names (`GetVideoFilePathAsync`, `ExtractAudioAsync`, etc.)
- Used `Task<T>` return types
- No standardized interface
- Direct async/await patterns

### Manual Workflow Orchestration

**VideoProcessingWorkflow.cs was a manual orchestrator:**

```csharp
public class VideoProcessingWorkflow
{
    private readonly FileInputExecutor _fileInputExecutor;
    private readonly AudioExtractionExecutor _audioExtractionExecutor;
    // ... all executors injected via DI

    public VideoProcessingWorkflow(
        FileInputExecutor fileInputExecutor,
        AudioExtractionExecutor audioExtractionExecutor,
        TranscriptionExecutor transcriptionExecutor,
        BlogGenerationExecutor blogGenerationExecutor,
        BlogEvaluationExecutor blogEvaluationExecutor,
        BlogPublishingExecutor blogPublishingExecutor,
        ILogger<VideoProcessingWorkflow> logger)
    {
        // Store all dependencies
    }

    public async Task<VideoProcessingResult> RunAsync()
    {
        // Step 1: Manual sequential execution
        var videoPath = await _fileInputExecutor.GetVideoFilePathAsync();
        
        // Step 2: Manual chaining
        var audioPath = await _audioExtractionExecutor.ExtractAudioAsync(videoPath);
        
        // Step 3: Continue manual chain
        var transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
        
        // Step 4-6: More manual chaining...
        var blogPost = await _blogGenerationExecutor.GenerateBlogPostAsync(transcript, videoPath);
        var evaluatedBlog = await _blogEvaluationExecutor.EvaluateBlogPostAsync(blogPost, videoPath);
        var result = await _blogPublishingExecutor.PublishBlogPostAsync(evaluatedBlog, videoPath);
        
        return new VideoProcessingResult { ... };
    }
}
```

**Key Characteristics:**
- Constructor-based dependency injection
- Manual sequential async/await calls
- Explicit try-catch error handling
- Custom result aggregation
- No framework-managed lifecycle

### Program.cs Execution

```csharp
// Register all executors as transient services
builder.Services.AddTransient<FileInputExecutor>();
builder.Services.AddTransient<AudioExtractionExecutor>();
// ... all other executors

// Register workflow orchestrator
builder.Services.AddTransient<VideoProcessingWorkflow>();

var host = builder.Build();

// Get workflow from DI and execute
var workflow = host.Services.GetRequiredService<VideoProcessingWorkflow>();
var result = await workflow.RunAsync();
```

**Key Characteristics:**
- Traditional DI registration
- Single `RunAsync()` method call
- No event handling
- No framework-managed execution

---

## What It Became: Microsoft Agent Framework Pattern (After)

### Framework-Based Executors

**All executors now inherit from `ReflectingExecutor<T>` and implement `IMessageHandler<TInput, TOutput>`:**

```csharp
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

public class FileInputExecutor : ReflectingExecutor<FileInputExecutor>, 
    IMessageHandler<string, string>
{
    private readonly ILogger<FileInputExecutor> _logger;

    public FileInputExecutor(ILogger<FileInputExecutor> logger) 
        : base("FileInput")
    {
        _logger = logger;
    }

    public ValueTask<string> HandleAsync(string input, IWorkflowContext context, 
        CancellationToken cancellationToken = default)
    {
        // Framework-compliant implementation
        return ValueTask.FromResult(filePath);
    }
}
```

**Key Changes:**
- ✅ Inherits from `ReflectingExecutor<FileInputExecutor>`
- ✅ Implements `IMessageHandler<string, string>` (input type, output type)
- ✅ Constructor calls `base("ExecutorId")`
- ✅ Standardized `HandleAsync` method signature
- ✅ Returns `ValueTask<TOutput>` instead of `Task<T>`
- ✅ Receives `IWorkflowContext` parameter for framework integration
- ✅ Receives `CancellationToken` for proper cancellation

### All Executor Transformations

| Executor | Input Type | Output Type | Framework Base |
|----------|------------|-------------|----------------|
| `FileInputExecutor` | `string` | `string` | `ReflectingExecutor<FileInputExecutor>` |
| `AudioExtractionExecutor` | `string` | `string` | `ReflectingExecutor<AudioExtractionExecutor>` |
| `TranscriptionExecutor` | `string` | `string` | `ReflectingExecutor<TranscriptionExecutor>` |
| `BlogGenerationExecutor` | `string` | `string` | `ReflectingExecutor<BlogGenerationExecutor>` |
| `BlogEvaluationExecutor` | `string` | `string` | `ReflectingExecutor<BlogEvaluationExecutor>` |
| `BlogPublishingExecutor` | `string` | `BlogPublishingResponse` | `ReflectingExecutor<BlogPublishingExecutor>` |

### WorkflowBuilder Pattern

**VideoProcessingWorkflow.cs transformed into a static factory:**

```csharp
using Microsoft.Agents.AI.Workflows;

public static class VideoProcessingWorkflow
{
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
```

**Key Changes:**
- ✅ Changed from instance class to static factory method
- ✅ Uses `WorkflowBuilder` to construct workflow graph
- ✅ Explicit edges define data flow (`AddEdge(from, to)`)
- ✅ `WithOutputFrom()` specifies which executors produce workflow output
- ✅ `Build()` creates immutable workflow instance
- ✅ No manual async/await chaining
- ✅ Framework validates type compatibility between edges

### Event-Driven Execution

**Program.cs now uses `InProcessExecution` with event handling:**

```csharp
using Microsoft.Agents.AI.Workflows;

// Register executors (same as before)
builder.Services.AddTransient<FileInputExecutor>();
builder.Services.AddTransient<AudioExtractionExecutor>();
// ... all other executors

var host = builder.Build();

// Get executors from DI container
var fileInputExecutor = host.Services.GetRequiredService<FileInputExecutor>();
var audioExtractionExecutor = host.Services.GetRequiredService<AudioExtractionExecutor>();
var transcriptionExecutor = host.Services.GetRequiredService<TranscriptionExecutor>();
var blogGenerationExecutor = host.Services.GetRequiredService<BlogGenerationExecutor>();
var blogEvaluationExecutor = host.Services.GetRequiredService<BlogEvaluationExecutor>();
var blogPublishingExecutor = host.Services.GetRequiredService<BlogPublishingExecutor>();

// Create workflow using WorkflowBuilder pattern
var workflow = VideoProcessingWorkflow.Create(
    fileInputExecutor,
    audioExtractionExecutor,
    transcriptionExecutor,
    blogGenerationExecutor,
    blogEvaluationExecutor,
    blogPublishingExecutor);

// Execute using framework's InProcessExecution
var run = await InProcessExecution.RunAsync(workflow, "start");

BlogPublishingResponse? finalResult = null;

// Process workflow events
foreach (WorkflowEvent evt in run.NewEvents)
{
    if (evt is ExecutorCompletedEvent completed)
    {
        Console.WriteLine($"✅ Completed: {completed.ExecutorId}");
    }
    else if (evt is WorkflowOutputEvent output)
    {
        if (output.Data is BlogPublishingResponse response)
        {
            finalResult = response;
        }
    }
}

// Display final results
if (finalResult != null)
{
    Console.WriteLine($"Status: {(finalResult.Success ? "Success" : "Failed")}");
    Console.WriteLine($"Message: {finalResult.Message}");
    if (finalResult.Success && !string.IsNullOrEmpty(finalResult.Result))
    {
        Console.WriteLine($"🔗 Published URL: {finalResult.Result}");
    }
}
```

**Key Changes:**
- ✅ Uses `InProcessExecution.RunAsync(workflow, input)` instead of custom `RunAsync()`
- ✅ Processes `WorkflowEvent` types for execution feedback
- ✅ `ExecutorCompletedEvent` - fired when each executor completes
- ✅ `WorkflowOutputEvent` - contains final workflow output
- ✅ Framework manages execution lifecycle
- ✅ Event-driven progress monitoring

---

## Key Differences Summary

| Aspect | Custom Pattern (Before) | Framework Pattern (After) |
|--------|------------------------|---------------------------|
| **Executor Base** | Plain classes | `ReflectingExecutor<T>` |
| **Interface** | None | `IMessageHandler<TInput, TOutput>` |
| **Method Name** | Custom (`GetVideoFilePathAsync`, etc.) | Standardized (`HandleAsync`) |
| **Return Type** | `Task<T>` | `ValueTask<T>` |
| **Parameters** | Varies by method | `(TInput, IWorkflowContext, CancellationToken)` |
| **Orchestration** | Manual async/await chaining | `WorkflowBuilder` with edges |
| **Data Flow** | Implicit (sequential calls) | Explicit (`.AddEdge(from, to)`) |
| **Execution** | Custom `RunAsync()` method | `InProcessExecution.RunAsync()` |
| **Progress Tracking** | Manual console output | Event-driven (`WorkflowEvent`) |
| **Type Safety** | Runtime errors | Compile-time validation |
| **Lifecycle** | Manual management | Framework-managed |
| **Error Handling** | Try-catch blocks | Framework-managed with events |

---

## Benefits of Framework Pattern

### 1. Type Safety
- **Before**: Runtime errors if output doesn't match next input
- **After**: Compile-time validation of type compatibility between executors

### 2. Explicit Data Flow
- **Before**: Data flow implicit in sequential method calls
- **After**: Visual workflow graph with explicit edges

### 3. Framework-Managed Execution
- **Before**: Manual async/await, error handling, progress tracking
- **After**: Framework handles execution model (Pregel-style supersteps)

### 4. Event Streaming
- **Before**: No visibility into execution progress
- **After**: Real-time events for monitoring and debugging

### 5. Standardization
- **Before**: Each executor had different method signatures
- **After**: All executors follow same `HandleAsync` pattern

### 6. Workflow Reusability
- **Before**: Workflow logic tightly coupled with executors
- **After**: Immutable workflow can be reused with different inputs

---

## Code Examples: Side-by-Side Comparison

### Executor Implementation

**Before (Custom Pattern):**
```csharp
public class TranscriptionExecutor
{
    private readonly ILogger<TranscriptionExecutor> _logger;
    private readonly IConfiguration _configuration;

    public TranscriptionExecutor(
        ILogger<TranscriptionExecutor> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<string> TranscribeAudioAsync(string audioPath)
    {
        // Custom implementation
        return transcript;
    }
}
```

**After (Framework Pattern):**
```csharp
public class TranscriptionExecutor : ReflectingExecutor<TranscriptionExecutor>, 
    IMessageHandler<string, string>
{
    private readonly ILogger<TranscriptionExecutor> _logger;
    private readonly IConfiguration _configuration;

    public TranscriptionExecutor(
        ILogger<TranscriptionExecutor> logger,
        IConfiguration configuration) : base("Transcription")
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async ValueTask<string> HandleAsync(string audioPath, 
        IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        // Framework-compliant implementation
        return transcript;
    }
}
```

### Workflow Construction

**Before (Manual Orchestration):**
```csharp
public async Task<VideoProcessingResult> RunAsync()
{
    var videoPath = await _fileInputExecutor.GetVideoFilePathAsync();
    var audioPath = await _audioExtractionExecutor.ExtractAudioAsync(videoPath);
    var transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
    var blogPost = await _blogGenerationExecutor.GenerateBlogPostAsync(transcript, videoPath);
    var evaluatedBlog = await _blogEvaluationExecutor.EvaluateBlogPostAsync(blogPost, videoPath);
    var result = await _blogPublishingExecutor.PublishBlogPostAsync(evaluatedBlog, videoPath);
    return result;
}
```

**After (WorkflowBuilder):**
```csharp
var workflow = new WorkflowBuilder(fileInputExecutor)
    .AddEdge(fileInputExecutor, audioExtractionExecutor)
    .AddEdge(audioExtractionExecutor, transcriptionExecutor)
    .AddEdge(transcriptionExecutor, blogGenerationExecutor)
    .AddEdge(blogGenerationExecutor, blogEvaluationExecutor)
    .AddEdge(blogEvaluationExecutor, blogPublishingExecutor)
    .WithOutputFrom(blogPublishingExecutor)
    .Build();

var run = await InProcessExecution.RunAsync(workflow, "start");
```

---

## Migration Checklist

- [x] Install `Microsoft.Agents.AI.Workflows` package (prerelease)
- [x] Update all executor base classes to `ReflectingExecutor<T>`
- [x] Implement `IMessageHandler<TInput, TOutput>` on all executors
- [x] Change method signatures to `ValueTask<TOutput> HandleAsync(TInput, IWorkflowContext, CancellationToken)`
- [x] Replace manual orchestration with `WorkflowBuilder` pattern
- [x] Update Program.cs to use `InProcessExecution.RunAsync()`
- [x] Implement event handling for `ExecutorCompletedEvent` and `WorkflowOutputEvent`
- [x] Update namespace imports to `Microsoft.Agents.AI.Workflows` and `Microsoft.Agents.AI.Workflows.Reflection`
- [x] Remove `[Handler]` attribute (not needed in this version)
- [x] Verify build succeeds

---

## Lessons Learned

### 1. Framework vs. Custom Implementation
- The custom pattern was "well-organized procedural code"
- The framework pattern provides actual workflow orchestration with:
  - Type validation
  - Event streaming
  - Pregel execution model
  - Workflow lifecycle management

### 2. "Orchestration" Terminology
- The term "orchestration" in AI/Agent context refers to:
  - Managing data flow between AI components
  - Coordinating multiple AI agents/models
  - Handling domain-specific patterns (retry, branching, aggregation)
- It's not a revolutionary computer science concept but a domain-specific pattern

### 3. ValueTask vs Task
- Framework uses `ValueTask<T>` for better performance
- Reduces allocations for synchronous completion paths
- Compatible with async/await

### 4. IWorkflowContext
- Provides access to workflow services
- Enables shared state between executors
- Allows custom event emission

---

## References

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [Create a Simple Sequential Workflow](https://learn.microsoft.com/en-us/agent-framework/tutorials/workflows/simple-sequential-workflow)
- [Workflow Core Concepts](https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/core-concepts/executors)
- Package: `Microsoft.Agents.AI.Workflows` v1.0.0-preview.251110.2

---

## Conclusion

This refactoring transformed the codebase from a custom executor pattern to the official **Microsoft Agent Framework Workflow** implementation. The new pattern provides:

- **Type Safety**: Compile-time validation of executor compatibility
- **Framework Benefits**: Managed execution, event streaming, lifecycle management
- **Standardization**: Consistent patterns across all executors
- **Explicit Flow**: Clear visual representation of workflow graph

The implementation now follows Microsoft's official patterns and can leverage future framework enhancements.
