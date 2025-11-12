# SemanticClip.Agents

A production-ready video-to-blog automation system built with **Microsoft Agent Framework**, Azure AI Foundry, and Model Context Protocol (MCP). This project demonstrates enterprise-grade AI workflow orchestration using the latest Microsoft AI technologies.

## 🎯 Overview

SemanticClip.Agents transforms video content into professionally formatted blog posts through an intelligent 6-step workflow:

1. **File Input** - Interactive video file selection with validation
2. **Audio Extraction** - High-quality audio extraction using FFmpeg
3. **Transcription** - Speech-to-text conversion with Azure OpenAI Whisper
4. **Blog Generation** - AI-powered content creation using Azure AI Foundry
5. **Blog Evaluation** - Quality enhancement with focus on SEO, engagement, and professional formatting
6. **Blog Publishing** - Automated GitHub publishing via Model Context Protocol

## 🏗️ Architecture

### Microsoft Agent Framework Implementation

This project showcases the **Microsoft Agent Framework** pattern with:

- **Executor Pattern**: Independent, single-responsibility executors for each workflow step
- **Workflow Orchestration**: `VideoProcessingWorkflow` coordinates all executors
- **Dependency Injection**: Clean DI architecture using `Microsoft.Extensions.Hosting`
- **Azure AI Foundry Integration**: Direct OpenAI client usage with Azure endpoints
- **MCP Integration**: GitHub publishing through Model Context Protocol

### Key Components

```
SemanticClip.Agents/
├── Executors/                    # Core processing units
│   ├── FileInputExecutor.cs      # User input and file validation
│   ├── AudioExtractionExecutor.cs # FFmpeg audio extraction
│   ├── TranscriptionExecutor.cs  # Azure OpenAI Whisper integration
│   ├── BlogGenerationExecutor.cs # AI blog post generation
│   ├── BlogEvaluationExecutor.cs # Content quality improvement
│   └── BlogPublishingExecutor.cs # GitHub publishing via MCP
├── Workflows/
│   └── VideoProcessingWorkflow.cs # Main orchestration workflow
├── Program.cs                     # Entry point with DI setup
└── appsettings.json              # Configuration
```

## 🚀 Getting Started

### Prerequisites

- .NET 9.0 SDK
- Azure OpenAI Service (for Whisper transcription)
- Azure AI Foundry endpoint (for blog generation/evaluation)
- GitHub Personal Access Token (for publishing)
- FFmpeg installed and in PATH
- Node.js/npm (for MCP GitHub server)

### Configuration

Update `appsettings.json` with your credentials:

```json
{
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.cognitiveservices.azure.com/",
    "ApiKey": "your-azure-openai-key",
    "WhisperDeploymentName": "whisper"
  },
  "AzureAIFoundry": {
    "Endpoint": "https://your-resource.services.ai.azure.com",
    "ApiKey": "your-foundry-key",
    "ChatDeploymentName": "gpt-4o"
  },
  "GitHub": {
    "PersonalAccessToken": "github_pat_...",
    "Repo": "username/repo-name"
  }
}
```

### Running the Application

```bash
cd SemanticClip.Agents
dotnet run
```

The workflow will:
1. Prompt for an MP4 video file
2. Extract and transcribe audio
3. Generate an initial blog post
4. Evaluate and improve the content
5. Publish to GitHub with a clickable URL

## 🔧 Technical Details

### Workflow Orchestration Pattern

The `VideoProcessingWorkflow` demonstrates a **sequential orchestration pattern** where each executor's output becomes the next executor's input:

```
Video File → Audio → Transcript → Blog Post → Evaluated Blog → Published URL
```

**Key Orchestration Features:**

1. **Sequential Execution**: Steps run in order with data flowing through the pipeline
2. **Error Propagation**: Failures at any step halt the workflow with clear error messages
3. **Graceful Degradation**: Optional steps are skipped if not configured
4. **State Tracking**: Results from each step are captured in `VideoProcessingResult`
5. **Resource Cleanup**: Temporary files are cleaned up in finally block

**Workflow Control Flow:**
```csharp
public async Task<VideoProcessingResult> RunAsync()
{
    try 
    {
        // Step 1: Input
        videoPath = await _fileInputExecutor.GetVideoFilePathAsync();
        
        // Step 2: Transform
        audioPath = await _audioExtractionExecutor.ExtractAudioAsync(videoPath);
        
        // Step 3: Transcribe
        if (_transcriptionExecutor.IsConfigured())
            transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
        
        // Step 4: Generate
        if (!string.IsNullOrWhiteSpace(transcript))
            blogPost = await _blogGenerationExecutor.GenerateBlogPostAsync(transcript, videoPath);
        
        // Step 5: Evaluate & Improve
        if (!string.IsNullOrWhiteSpace(blogPost))
            evaluatedBlogPost = await _blogEvaluationExecutor.EvaluateBlogPostAsync(blogPost, videoPath);
        
        // Step 6: Publish
        if (!string.IsNullOrWhiteSpace(evaluatedBlogPost ?? blogPost))
            publishResponse = await _blogPublishingExecutor.PublishBlogPostAsync(evaluatedBlogPost ?? blogPost, videoPath);
    }
    finally 
    {
        // Cleanup resources
    }
}
```

### Executor Pattern

Each executor is a self-contained unit with:
- Configuration validation via `IsConfigured()`
- Async processing methods
- Comprehensive error handling
- Progress reporting to console

Example:
```csharp
public class BlogGenerationExecutor
{
    public bool IsConfigured() => _openAIClient != null;
    
    public async Task<string> GenerateBlogPostAsync(
        string transcript, 
        string videoFilePath)
    {
        // Implementation
    }
}
```

### Azure AI Foundry Integration

Uses the OpenAI SDK with Azure AI Foundry endpoints:

```csharp
var foundryEndpoint = endpoint.TrimEnd('/') + "/openai/v1/";
var clientOptions = new OpenAIClientOptions() 
{ 
    Endpoint = new Uri(foundryEndpoint) 
};
_openAIClient = new OpenAIClient(
    new System.ClientModel.ApiKeyCredential(apiKey), 
    clientOptions);
```

### Model Context Protocol (MCP)

Direct tool invocation for GitHub operations:

```csharp
var mcpClient = await McpClientFactory.CreateAsync(
    new StdioClientTransport(new StdioClientTransportOptions
    {
        Name = "GitHub",
        Command = "npx",
        Arguments = ["-y", "@modelcontextprotocol/server-github"],
        EnvironmentVariables = new Dictionary<string, string>
        {
            { "GITHUB_PERSONAL_ACCESS_TOKEN", githubToken }
        }
    }));

var callResult = await mcpClient.CallToolAsync(
    "create_or_update_file",
    new Dictionary<string, object?> { /* parameters */ });
```

## 📦 Dependencies

### Core Packages
- `Microsoft.Agents.AI.OpenAI` (1.0.0-preview.251110.2) - Agent Framework
- `Azure.AI.OpenAI` (2.1.0) - Azure OpenAI integration
- `OpenAI` (2.1.0) - OpenAI SDK for Azure AI Foundry
- `ModelContextProtocol` (0.1.0-preview.11) - MCP client
- `Azure.Identity` (1.17.0) - Azure authentication

### Microsoft Extensions
- `Microsoft.Extensions.Hosting` (10.0.0)
- `Microsoft.Extensions.Configuration` (10.0.0)
- `Microsoft.Extensions.Logging` (10.0.0)
- `Microsoft.Extensions.DependencyInjection` (10.0.0)

## 🎨 Features

### Intelligent Content Processing
- **Multi-stage AI workflow** with quality gates at each step
- **Professional markdown formatting** with proper heading hierarchy
- **SEO optimization** during evaluation phase
- **Content enhancement** for clarity, engagement, and technical accuracy

### Production-Ready Design
- **Comprehensive error handling** with graceful degradation
- **Progress reporting** with visual indicators
- **Configuration validation** before execution
- **Automatic cleanup** of temporary files
- **Detailed logging** for debugging and monitoring

### GitHub Integration
- **Automated publishing** to specified repository
- **Timestamped filenames** for organization
- **Clickable URLs** in console output
- **Commit messages** with context

## 📊 Workflow Output

```
╔══════════════════════════════════════════════════════════════╗
║        SemanticClip - Agent Framework Video Processor       ║
╚══════════════════════════════════════════════════════════════╝

📋 Step 1: File Selection
📋 Step 2: Audio Extraction
📋 Step 3: Audio Transcription
📋 Step 4: Blog Post Generation
📋 Step 5: Blog Post Evaluation
📋 Step 6: Blog Publishing

╔══════════════════════════════════════════════════════════════╗
║                    ✅ Workflow Complete!                     ║
╚══════════════════════════════════════════════════════════════╝

📊 Summary:
   Video File: C:\Temp\video.mp4
   Status: Blog post published to GitHub successfully!
   🔗 Published URL: https://github.com/user/repo/blob/main/blog-posts/video-20251112-095019.md
```

## 🔐 Security Notes

- Store sensitive keys in Azure Key Vault or User Secrets in production
- Use Managed Identity for Azure service authentication when possible
- Never commit `appsettings.json` with actual credentials to version control
- Rotate GitHub tokens regularly

## 📝 Migration from Semantic Kernel

This project demonstrates the evolution from **event-driven process orchestration** to **sequential workflow orchestration**:

### Orchestration Patterns Comparison

| Aspect | Semantic Kernel Process Framework | Microsoft Agent Framework |
|--------|----------------------------------|---------------------------|
| **Orchestration Model** | Event-driven with message passing | Sequential pipeline |
| **Flow Control** | Process events and step routing | Direct async/await chaining |
| **State Management** | `KernelProcessStepState<T>` | Simple instance variables |
| **Step Communication** | `EmitEventAsync()` / `OnFunctionResult()` | Return values / method parameters |
| **Complexity** | Higher (event routing, targets) | Lower (direct method calls) |
| **Debugging** | Complex (trace events) | Simple (step through code) |
| **Testing** | Unit test individual steps | Test executors independently |
| **Extensibility** | Add steps + wire events | Add executor + call in workflow |

### Example: From Process to Workflow

**Before (Semantic Kernel):**
```csharp
var transcribeStep = processBuilder.AddStepFromType<TranscribeVideoStep>();
var generateStep = processBuilder.AddStepFromType<GenerateBlogPostStep>();

transcribeStep
    .OnFunctionResult()
    .SendEventTo(new ProcessFunctionTargetBuilder(generateStep,
        functionName: GenerateBlogPostStep.Functions.GenerateBlogPost,
        parameterName: "transcript"));
```

**After (Agent Framework):**
```csharp
var transcript = await _transcriptionExecutor.TranscribeAudioAsync(audioPath);
var blogPost = await _blogGenerationExecutor.GenerateBlogPostAsync(transcript, videoPath);
```

### When to Use Each Pattern

**Use Sequential Workflow (Agent Framework)** when:
- Steps execute in a predictable order
- Each step depends on the previous step's output
- You want simpler debugging and testing
- The workflow is relatively linear

**Use Event-Driven Process (Semantic Kernel)** when:
- Steps can run in parallel or conditional branches
- Complex routing logic based on step outcomes
- Need to support dynamic workflow modification
- Building a workflow engine or orchestrator platform

## 🤝 Contributing

This is a reference implementation showcasing Microsoft Agent Framework best practices. Feel free to adapt the patterns for your own projects.

## 📄 License

[Your License Here]

## 🙏 Acknowledgments

- Microsoft Agent Framework Team
- Azure AI Platform
- Model Context Protocol (Anthropic)
- FFmpeg Project

## Overview

This is a reimplementation of SemanticClip using executor-based patterns inspired by Microsoft Agent Framework, designed to replace the Semantic Kernel Process Framework approach with a more straightforward workflow orchestration.

### Current Architecture

```
┌──────────────────────┐
│  FileInputExecutor   │  ← Prompts user for MP4 file, validates existence
└──────────┬───────────┘
           │
           ▼
┌──────────────────────┐
│AudioExtractionExecutor│ ← Extracts audio using FFmpeg
└──────────┬───────────┘
           │
           ▼
    [Future Steps]
    - Transcription
    - Blog Generation
    - Publishing
```

## Key Components

### Executors (`/Executors`)
Individual units of work that perform specific tasks:

- **FileInputExecutor**: Handles user interaction for file selection and validation
- **AudioExtractionExecutor**: Uses FFmpeg to extract audio from video files

### Workflows (`/Workflows`)
Orchestrators that coordinate multiple executors:

- **VideoProcessingWorkflow**: Main workflow that chains executors together

### Comparison with Semantic Kernel Process Framework

| Aspect | Semantic Kernel Process | Agent Framework Pattern |
|--------|------------------------|------------------------|
| Base Pattern | `KernelProcessStep<T>` | Simple executor classes |
| Orchestration | `ProcessBuilder` with events | Direct method chaining |
| State Management | `KernelProcessStepState` | Instance variables |
| Execution | Event-driven async | Sequential async/await |
| Complexity | Higher (event routing) | Lower (direct calls) |

## Prerequisites

1. **.NET 9 SDK**
2. **FFmpeg** installed and in PATH
   ```powershell
   # Windows (Chocolatey)
   choco install ffmpeg
   
   # Or download from https://ffmpeg.org/download.html
   ```
3. **Azure AI Services** (for future transcription steps)

## Setup

1. **Restore packages**:
   ```powershell
   dotnet restore
   ```

2. **Configure Azure AI** (optional for now):
   Edit `appsettings.json` or use user secrets:
   ```powershell
   dotnet user-secrets set "AzureAI:Endpoint" "https://your-endpoint.openai.azure.com/"
   dotnet user-secrets set "AzureAI:ModelDeployment" "gpt-4o"
   ```

3. **Run the application**:
   ```powershell
   dotnet run
   ```

## Project Structure

```
SemanticClip.Agents/
├── Executors/
│   ├── FileInputExecutor.cs          # User input & validation
│   └── AudioExtractionExecutor.cs    # FFmpeg audio extraction
├── Workflows/
│   └── VideoProcessingWorkflow.cs    # Main orchestrator
├── Program.cs                         # Entry point with DI
├── appsettings.json                   # Configuration
└── SemanticClip.Agents.csproj        # Project file
```

## Usage

When you run the application:

1. You'll be prompted to enter a path to an MP4 video file
2. The file will be validated
3. Audio will be extracted using FFmpeg to a temporary WAV file
4. [Future] Audio will be transcribed using Azure OpenAI Whisper
5. [Future] Blog post will be generated
6. Temporary files will be cleaned up

### Example Session

```
╔══════════════════════════════════════════════════════════════╗
║        SemanticClip - Agent Framework Video Processor       ║
╚══════════════════════════════════════════════════════════════╝

📋 Step 1: File Selection
──────────────────────────────────────────────────────────────
Enter the path to your MP4 video file: C:\Videos\demo.mp4
✅ File validated: demo.mp4 (25.4 MB)

📋 Step 2: Audio Extraction
──────────────────────────────────────────────────────────────
🎬 Extracting audio from: demo.mp4
⏳ Running FFmpeg (this may take a moment)...
✅ Audio extracted successfully: 1024.50 KB

📋 Step 3: Audio Transcription (Coming Soon)
──────────────────────────────────────────────────────────────
⏳ Transcription step not yet implemented
...
```

## Extending the Workflow

To add new processing steps:

1. **Create a new Executor** in `/Executors`:
   ```csharp
   public class TranscriptionExecutor
   {
       public async Task<string> TranscribeAsync(string audioPath)
       {
           // Implementation
       }
   }
   ```

2. **Register in Program.cs**:
   ```csharp
   builder.Services.AddTransient<TranscriptionExecutor>();
   ```

3. **Add to VideoProcessingWorkflow**:
   ```csharp
   var transcript = await _transcriptionExecutor.TranscribeAsync(audioPath);
   ```

## Implementation Status

- ✅ File input and validation
- ✅ FFmpeg audio extraction
- ✅ Dependency injection setup
- ✅ Logging infrastructure
- ⏳ Azure OpenAI Whisper transcription (planned)
- ⏳ Blog post generation (planned)
- ⏳ GitHub publishing (planned)

## Next Steps

1. Implement **TranscriptionExecutor** using Azure OpenAI Whisper
2. Implement **BlogGenerationExecutor** using Azure OpenAI or local models
3. Implement **PublishingExecutor** for GitHub integration
4. Add error recovery and retry logic
5. Add progress reporting for long-running operations

## Resources

- [Microsoft Agent Framework](https://github.com/microsoft/agent-framework)
- [Azure AI Services](https://azure.microsoft.com/en-us/products/ai-services/)
- [FFmpeg Documentation](https://ffmpeg.org/documentation.html)
