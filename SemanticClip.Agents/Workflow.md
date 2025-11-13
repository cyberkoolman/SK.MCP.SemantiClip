# Workflow Architecture & Orchestration

This document explains the **workflow orchestration pattern** used in SemanticClip.Agents and how it differs from traditional process frameworks.

## 🎯 Core Concepts

### Executors

**Executors** are independent, single-responsibility units that perform a specific task in the workflow. Think of them as specialized workers, each with one job.

**Key Characteristics:**
- **Stateless**: No shared state between executions
- **Self-contained**: All dependencies injected via constructor
- **Validatable**: `IsConfigured()` checks if executor can run
- **Async**: All operations are async for scalability
- **Error-aware**: Handles exceptions gracefully with logging

**Executor Example:**
```csharp
public class TranscriptionExecutor
{
    private readonly ILogger<TranscriptionExecutor> _logger;
    private readonly IConfiguration _configuration;
    private readonly AzureOpenAIClient? _openAIClient;

    // Constructor injection
    public TranscriptionExecutor(ILogger<TranscriptionExecutor> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        // Initialize Azure OpenAI client
    }

    // Validation
    public bool IsConfigured() => _openAIClient != null;

    // Single responsibility: transcribe audio
    public async Task<string> TranscribeAudioAsync(string audioPath)
    {
        // Transcription logic
    }
}
```

### Workflow Orchestrator

The **Workflow Orchestrator** (`VideoProcessingWorkflow`) coordinates executors, managing the data flow and execution sequence.

**Responsibilities:**
- **Sequence Control**: Determine order of executor invocations
- **Data Flow**: Pass outputs from one executor as inputs to the next
- **Error Handling**: Catch and report failures
- **State Management**: Track results from each step
- **Resource Cleanup**: Ensure temporary resources are released

**Orchestration Pattern:**
```csharp
public class VideoProcessingWorkflow
{
    // Executors injected via DI
    private readonly FileInputExecutor _fileInputExecutor;
    private readonly AudioExtractionExecutor _audioExtractionExecutor;
    private readonly TranscriptionExecutor _transcriptionExecutor;
    // ... more executors

    public async Task<VideoProcessingResult> RunAsync()
    {
        // Step 1: Input
        var videoPath = await _fileInputExecutor.GetVideoFilePathAsync();
        
        // Step 2: Transform - output of step 1 becomes input of step 2
        var audioPath = await _audioExtractionExecutor.ExtractAudioAsync(videoPath);
        
        // Step 3: Process - conditional execution
        if (_transcriptionExecutor.IsConfigured())
            var transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
        
        // ... continue chain
    }
}
```

## 🔗 Edges (Data Flow)

**Edges** represent the connections between executors - how data flows from one step to the next.

### In Traditional Process Frameworks
Edges are **explicit** event-based connections:
```csharp
// Semantic Kernel Process Framework
transcribeStep
    .OnFunctionResult()  // Event triggered
    .SendEventTo(generateStep, parameterName: "transcript");  // Explicit edge
```

### In Agent Framework Pattern
Edges are **implicit** through direct method calls and return values:
```csharp
// Microsoft Agent Framework Pattern
var transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
var blogPost = await _blogGenerationExecutor.GenerateBlogPostAsync(transcript, videoPath);
//            ↑ implicit edge: transcript flows from transcription to generation
```

## 📊 Workflow Visualization

### SemanticClip.Agents Workflow Graph

```mermaid
graph TD
    A[FileInputExecutor] -->|videoPath: string| B[AudioExtractionExecutor]
    B -->|audioPath: string| C[TranscriptionExecutor]
    C -->|transcript: string| D[BlogGenerationExecutor]
    D -->|blogPost: string| E[BlogEvaluationExecutor]
    E -->|evaluatedBlogPost: string| F[BlogPublishingExecutor]
    F -->|publishResponse: BlogPublishingResponse| G[GitHub URL]
    
    style A fill:#e1f5ff
    style B fill:#ffe1f5
    style C fill:#f5ffe1
    style D fill:#fff5e1
    style E fill:#e1fff5
    style F fill:#f5e1ff
    style G fill:#90EE90
```

### Detailed Flow Diagram

```mermaid
sequenceDiagram
    participant User
    participant Workflow as VideoProcessingWorkflow
    participant FI as FileInputExecutor
    participant AE as AudioExtractionExecutor
    participant TE as TranscriptionExecutor
    participant BG as BlogGenerationExecutor
    participant BE as BlogEvaluationExecutor
    participant BP as BlogPublishingExecutor
    participant GitHub

    User->>Workflow: RunAsync()
    Workflow->>FI: GetVideoFilePathAsync()
    FI->>User: Prompt for file path
    User-->>FI: C:\Temp\video.mp4
    FI-->>Workflow: videoPath
    
    Workflow->>AE: ExtractAudioAsync(videoPath)
    AE->>AE: Run FFmpeg
    AE-->>Workflow: audioPath
    
    Workflow->>TE: TranscribeAudioAsync(audioPath)
    TE->>TE: Azure OpenAI Whisper
    TE-->>Workflow: transcript
    
    Workflow->>BG: GenerateBlogPostAsync(transcript)
    BG->>BG: Azure AI Foundry (gpt-4o)
    BG-->>Workflow: blogPost
    
    Workflow->>BE: EvaluateBlogPostAsync(blogPost)
    BE->>BE: AI Quality Improvement
    BE-->>Workflow: evaluatedBlogPost
    
    Workflow->>BP: PublishBlogPostAsync(evaluatedBlogPost)
    BP->>GitHub: MCP create_or_update_file
    GitHub-->>BP: File created
    BP-->>Workflow: publishResponse
    
    Workflow-->>User: VideoProcessingResult + URL
```

### Executor Dependency Graph

```mermaid
graph LR
    subgraph "Dependency Injection Container"
        Config[IConfiguration]
        Logger[ILogger]
    end
    
    subgraph "Executors"
        FI[FileInputExecutor]
        AE[AudioExtractionExecutor]
        TE[TranscriptionExecutor]
        BG[BlogGenerationExecutor]
        BE[BlogEvaluationExecutor]
        BP[BlogPublishingExecutor]
    end
    
    subgraph "Workflow"
        VPW[VideoProcessingWorkflow]
    end
    
    Config --> FI
    Config --> AE
    Config --> TE
    Config --> BG
    Config --> BE
    Config --> BP
    
    Logger --> FI
    Logger --> AE
    Logger --> TE
    Logger --> BG
    Logger --> BE
    Logger --> BP
    
    FI --> VPW
    AE --> VPW
    TE --> VPW
    BG --> VPW
    BE --> VPW
    BP --> VPW
    
    style Config fill:#FFE4B5
    style Logger fill:#FFE4B5
    style VPW fill:#87CEEB
```

### State Flow Diagram

```mermaid
stateDiagram-v2
    [*] --> FileInput: User starts workflow
    FileInput --> AudioExtraction: video file validated
    AudioExtraction --> Transcription: audio extracted
    Transcription --> BlogGeneration: transcript created
    BlogGeneration --> BlogEvaluation: initial blog generated
    BlogEvaluation --> Publishing: blog improved
    Publishing --> Success: published to GitHub
    Publishing --> PartialSuccess: evaluation completed
    Success --> [*]: URL returned
    PartialSuccess --> [*]: blog saved locally
    
    FileInput --> Error: invalid file
    AudioExtraction --> Error: FFmpeg failed
    Transcription --> BlogGeneration: not configured (skip)
    BlogGeneration --> BlogEvaluation: not configured (skip)
    BlogEvaluation --> Publishing: not configured (use original)
    Error --> [*]: workflow failed
    
    note right of Transcription
        Optional step
        Graceful degradation
    end note
    
    note right of BlogEvaluation
        Improves quality
        SEO optimization
    end note
```

### Event-Driven Process (Semantic Kernel)

**Structure:**
```
Process Builder
├── Define Steps
├── Wire Events (create edges)
├── Configure Routing
└── Execute Process
    ├── Step emits event
    ├── Event router finds target
    ├── Target step receives event
    └── Repeat
```

**Pros:**
- Supports complex branching and parallel execution
- Dynamic workflow modification at runtime
- Decoupled steps (don't know about each other)
- Built-in state management

**Cons:**
- Higher complexity
- Harder to debug (trace events)
- More boilerplate code
- Steeper learning curve

### Sequential Workflow (Agent Framework)

**Structure:**
```
Workflow Orchestrator
├── Inject Executors
└── Execute Sequence
    ├── Call executor method
    ├── Get result
    ├── Pass to next executor
    └── Repeat
```

**Pros:**
- Simple and intuitive
- Easy to debug (step through code)
- Minimal boilerplate
- Clear data flow

**Cons:**
- Limited to sequential or simple branching
- Manual state management
- Harder to parallelize
- Tight coupling in orchestrator

## 🎨 Execution Patterns

### 1. Sequential Execution
```csharp
var result1 = await _executor1.RunAsync(input);
var result2 = await _executor2.RunAsync(result1);
var result3 = await _executor3.RunAsync(result2);
```

### 2. Conditional Execution
```csharp
var result1 = await _executor1.RunAsync(input);

if (result1.RequiresProcessing)
    var result2 = await _executor2.RunAsync(result1);
else
    var result2 = await _executor3.RunAsync(result1);
```

### 3. Optional Steps
```csharp
var result1 = await _executor1.RunAsync(input);

// Skip if not configured
if (_executor2.IsConfigured())
    var result2 = await _executor2.RunAsync(result1);
```

### 4. Error Handling with Fallback
```csharp
var result1 = await _executor1.RunAsync(input);

try 
{
    var result2 = await _executor2.RunAsync(result1);
}
catch (Exception ex)
{
    _logger.LogWarning("Step 2 failed, using fallback");
    var result2 = FallbackResult;
}
```

### 5. Parallel Execution (Advanced)
```csharp
var result1 = await _executor1.RunAsync(input);

// Fork: run two executors in parallel
var task2 = _executor2.RunAsync(result1);
var task3 = _executor3.RunAsync(result1);
await Task.WhenAll(task2, task3);

// Join: combine results
var result4 = await _executor4.RunAsync(task2.Result, task3.Result);
```

## 🔧 Implementation Details

### Executor Lifecycle Diagram

```mermaid
sequenceDiagram
    participant DI as DI Container
    participant Ctor as Constructor
    participant Workflow
    participant Executor
    participant Azure as Azure/External Service
    
    DI->>Ctor: New instance requested
    Ctor->>Ctor: Initialize fields
    Ctor->>Ctor: Read configuration
    Ctor->>Ctor: Create clients (if configured)
    Ctor-->>DI: Return instance
    
    DI->>Workflow: Inject executor
    Workflow->>Executor: IsConfigured()?
    Executor-->>Workflow: true/false
    
    alt Configured
        Workflow->>Executor: ExecuteAsync(input)
        Executor->>Azure: API call
        Azure-->>Executor: Response
        Executor-->>Workflow: Result
    else Not Configured
        Workflow->>Workflow: Skip step or use fallback
    end
    
    Note over Executor: Garbage collected when workflow completes
```

### Workflow Modification Patterns

```mermaid
graph TD
    subgraph "Pattern 1: Insert Step"
        I1[Step A] -->|result| I2[NEW Step B]
        I2 -->|result| I3[Step C]
    end
    
    subgraph "Pattern 2: Optional Step"
        O1[Step A] -->|result| O2{IsConfigured?}
        O2 -->|Yes| O3[Optional Step]
        O2 -->|No| O4[Skip to Next]
        O3 --> O4
        O4 --> O5[Step C]
    end
    
    subgraph "Pattern 3: Branching"
        B1[Step A] --> B2{Condition?}
        B2 -->|Path 1| B3[Step B1]
        B2 -->|Path 2| B4[Step B2]
        B3 --> B5[Step C]
        B4 --> B5
    end
    
    subgraph "Pattern 4: Parallel Fork"
        P1[Step A] --> P2[Step B1]
        P1 --> P3[Step B2]
        P2 --> P4[Merge Results]
        P3 --> P4
        P4 --> P5[Step C]
    end
    
    style I2 fill:#90EE90
    style O3 fill:#FFE4B5
    style B3 fill:#87CEEB
    style B4 fill:#DDA0DD
    style P2 fill:#F0E68C
    style P3 fill:#F0E68C
```

### Adding a New Executor

1. **Create the executor class** in `/Executors`:
```csharp
public class NewExecutor
{
    public async Task<OutputType> ProcessAsync(InputType input)
    {
        // Implementation
    }
}
```

2. **Register in DI** in `Program.cs`:
```csharp
builder.Services.AddTransient<NewExecutor>();
```

3. **Inject into workflow**:
```csharp
public VideoProcessingWorkflow(
    // ... existing executors
    NewExecutor newExecutor)
{
    _newExecutor = newExecutor;
}
```

4. **Call in sequence**:
```csharp
var newResult = await _newExecutor.ProcessAsync(previousResult);
```

### Modifying Workflow Flow

**Insert a step:**
```csharp
var result1 = await _executor1.RunAsync(input);
var result2 = await _newExecutor.RunAsync(result1);  // New step
var result3 = await _executor3.RunAsync(result2);
```

**Make a step optional:**
```csharp
var result2 = result1;  // Default to previous result
if (_optionalExecutor.IsConfigured())
    result2 = await _optionalExecutor.RunAsync(result1);
```

**Add branching:**
```csharp
if (result1.NeedsPathA)
    var result2 = await _executorA.RunAsync(result1);
else
    var result2 = await _executorB.RunAsync(result1);
```

## 🎓 Summary

**Executors** are the workers - specialized, independent units that do one thing well.

**Edges** are the connections - data flowing from one executor to the next (implicit via method parameters and return values).

**Workflows** are the managers - coordinating executors in the right sequence with proper error handling and cleanup.

This pattern provides a **simple, debuggable, and maintainable** approach to workflow orchestration, ideal for sequential AI processing pipelines like video-to-blog transformation.
