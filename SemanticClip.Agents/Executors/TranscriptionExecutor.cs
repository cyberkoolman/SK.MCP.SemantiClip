using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Audio;

namespace SemanticClip.Agents.Executors;

/// <summary>
/// Executor that transcribes audio files using Azure OpenAI Whisper.
/// Ported from the Semantic Kernel TranscribeVideoStep to use Microsoft Agent Framework.
/// </summary>
public class TranscriptionExecutor : ReflectingExecutor<TranscriptionExecutor>, IMessageHandler<string, string>
{
    private readonly ILogger<TranscriptionExecutor> _logger;
    private readonly IConfiguration _configuration;
    private readonly AzureOpenAIClient? _openAIClient;

    public TranscriptionExecutor(
        ILogger<TranscriptionExecutor> logger,
        IConfiguration configuration) : base("Transcription")
    {
        _logger = logger;
        _configuration = configuration;

        // Initialize Azure OpenAI client
        var endpoint = _configuration["AzureOpenAI:Endpoint"];
        var apiKey = _configuration["AzureOpenAI:ApiKey"];

        if (!string.IsNullOrEmpty(endpoint) && !string.IsNullOrEmpty(apiKey))
        {
            _openAIClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new AzureKeyCredential(apiKey));
        }
        else if (!string.IsNullOrEmpty(endpoint))
        {
            // Try DefaultAzureCredential if no API key is provided
            _openAIClient = new AzureOpenAIClient(
                new Uri(endpoint),
                new DefaultAzureCredential());
        }
    }

    /// <summary>
    /// Transcribes an audio file using Azure OpenAI Whisper.
    /// </summary>
    /// <param name="audioPath">Path to the audio file (WAV format)</param>
    /// <returns>Transcribed text</returns>
    public async ValueTask<string> HandleAsync(string audioPath, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        if (_openAIClient == null)
        {
            throw new InvalidOperationException(
                "Azure OpenAI client is not configured. Please check your appsettings.json for AzureOpenAI:Endpoint and AzureOpenAI:ApiKey");
        }

        _logger.LogDebug("Starting audio transcription: {AudioPath}", audioPath);
        Console.WriteLine($"\n🎤 Transcribing audio: {Path.GetFileName(audioPath)}");

        try
        {
            var deploymentName = _configuration["AzureOpenAI:WhisperDeploymentName"] ?? "whisper";
            
            // Read the audio file
            using var audioFileStream = File.OpenRead(audioPath);
            
            // Get the audio client
            var audioClient = _openAIClient.GetAudioClient(deploymentName);
            
            Console.WriteLine("⏳ Calling Azure OpenAI Whisper API...");
            
            // Transcribe the audio
            var transcriptionOptions = new AudioTranscriptionOptions
            {
                ResponseFormat = AudioTranscriptionFormat.Text,
                Temperature = 0.0f // Set to 0 for more deterministic results
            };

            AudioTranscription transcription = await audioClient.TranscribeAudioAsync(
                audioFileStream,
                Path.GetFileName(audioPath),
                transcriptionOptions);

            var transcript = transcription.Text;

            if (string.IsNullOrWhiteSpace(transcript))
            {
                throw new InvalidOperationException("Transcription returned empty result");
            }

            _logger.LogDebug("Transcription completed successfully. Length: {Length} characters", transcript.Length);
            Console.WriteLine($"✅ Transcription completed: {transcript.Length} characters");
            
            // Print the full transcript
            Console.WriteLine($"\n📝 Full Transcript:\n");
            Console.WriteLine(new string('─', 60));
            Console.WriteLine(transcript);
            Console.WriteLine(new string('─', 60));

            return transcript;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during audio transcription");
            Console.WriteLine($"❌ Transcription failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Validates that Azure OpenAI is configured
    /// </summary>
    public bool IsConfigured()
    {
        return _openAIClient != null;
    }
}
