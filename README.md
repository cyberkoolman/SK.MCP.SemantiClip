# SemantiClip.Console

> **Note**: This is a proof of concept application based on the Microsoft Repo https://github.com/vicperdana/SemantiClip.

**Difference**: Added Console support to cut down unnecessary UI components just to focus on AI piece, this converts videos into structured content by transcribing audio and creating blog posts. Built with .NET, **Microsoft Semantic Kernel Agent and Process Frameworks** and Azure Open AI Foundry.

<p align="center">
  <img src="docs/images/SemantiClip-Overview.png" alt="SemantiClip Overview">
</p>

### Key Features
This transforms video content into rich, structured written formats, automates the transcription, segmentation, and content generation process through sophisticated AI agent orchestration using both Small Language Models (SLM) and Large Language Models (LLM).

- 🎙️ **Audio Extraction** – Uses FFmpeg to extract audio from video files.
- ✍️ **Transcription** – Converts speech to text using Azure OpenAI Whisper.
- 📝 **Blog Post Creation** – Automatically generates readable blog posts from transcripts.
- 🧩 **Local Content Generation** – Supports on-device LLM processing with Ollama.
- 🔍 **Semantic Kernel Integration** – Utilizes Semantic Kernel Process and Agent frameworks for enhanced context and orchestration.
- 📗 **GitHub with ModelContextProtocol Integration** – Publishes blog posts directly to GitHub repositories with ModelContextProtocol.

## Reference
- Architecture: [Architecture Overview](Architecture.md)
- Process Framework: [Semantic Kernel Process Framework Overview](process_framework.md)


### Configuration

1. Configure GitHub Integration
   ```bash
   # Create a GitHub personal access token with repo access - see more details [here](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens#creating-a-fine-grained-personal-access-token)
   # Copy the generated token and add it to your appsettings.json 
   "GitHub": {
    "PersonalAccessToken": "yourGitHubToken"
    },
   ```
   
2. Install FFmpeg
   ```bash
   # macOS
   brew install ffmpeg
   
   # Ubuntu
   sudo apt-get install ffmpeg
   
   # Windows
   # Download from https://ffmpeg.org/download.html and add to PATH
   ```

3. Configure `appsettings.Development.json` under the SemanticClip.API project
   ```json
   {
     "AzureOpenAI": {
       "Endpoint": "your-azure-openai-endpoint",
       "ApiKey": "your-azure-openai-api-key",
     },
     "LocalSLM": {
       "ModelId": "phi4-mini",
       "Endpoint": "http://localhost:11434"
     },
     "AzureAIAgent": {
       "ConnectionString": "your-azure-ai-agent-connection-string",
       "ChatModelId": "gpt-4o",
       "VectorStoreId": "semanticclipproject",
       "MaxEvaluations": "3"
   }
   ```

4. Run the application
   ```bash
   cd SemanticClip.Console
   dotnet run
   ```