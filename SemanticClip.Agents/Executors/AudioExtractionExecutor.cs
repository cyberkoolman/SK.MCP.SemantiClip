using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.Logging;

namespace SemanticClip.Agents.Executors;

/// <summary>
/// Executor that extracts audio from a video file using FFmpeg.
/// Ported from the Semantic Kernel TranscribeVideoStep.
/// </summary>
public class AudioExtractionExecutor : ReflectingExecutor<AudioExtractionExecutor>, IMessageHandler<string, string>
{
    private readonly ILogger<AudioExtractionExecutor> _logger;

    public AudioExtractionExecutor(ILogger<AudioExtractionExecutor> logger) : base("AudioExtraction")
    {
        _logger = logger;
    }

    /// <summary>
    /// Extracts audio from the provided video file using FFmpeg.
    /// Returns the path to the extracted audio file (WAV format).
    /// </summary>
    /// <param name="videoPath">Full path to the video file</param>
    /// <returns>Path to the extracted audio file</returns>
    public async ValueTask<string> HandleAsync(string videoPath, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting audio extraction from video: {VideoPath}", videoPath);
        Console.WriteLine($"\n🎬 Extracting audio from: {Path.GetFileName(videoPath)}");

        // Generate output path for the audio file
        var outputAudioPath = Path.Combine(
            Path.GetTempPath(), 
            $"SemanticClip_{Guid.NewGuid()}.wav"
        );

        try
        {
            await ExtractAudioFromVideoAsync(videoPath, outputAudioPath);
            
            var audioFileInfo = new FileInfo(outputAudioPath);
            _logger.LogDebug("Audio extraction completed: {AudioPath} ({Size} bytes)", 
                outputAudioPath, audioFileInfo.Length);
            
            Console.WriteLine($"✅ Audio extracted successfully: {audioFileInfo.Length / 1024.0:F2} KB");
            
            return outputAudioPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during audio extraction");
            
            // Cleanup on failure
            if (File.Exists(outputAudioPath))
            {
                try
                {
                    File.Delete(outputAudioPath);
                }
                catch { /* Ignore cleanup errors */ }
            }
            
            throw;
        }
    }

    /// <summary>
    /// Uses FFmpeg to extract audio from video file.
    /// Ported from SemanticClip.Services TranscribeVideoStep.
    /// </summary>
    private async Task ExtractAudioFromVideoAsync(string videoPath, string outputAudioPath)
    {
        _logger.LogDebug("Running FFmpeg to extract audio...");

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                // FFmpeg arguments:
                // -i: input file
                // -vn: disable video
                // -acodec pcm_s16le: audio codec (PCM 16-bit little-endian)
                // -ar 16000: audio sampling rate (16kHz, good for speech recognition)
                // -ac 1: audio channels (mono)
                // -y: overwrite output file if exists
                Arguments = $"-i \"{videoPath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputAudioPath}\" -y",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null) outputBuilder.AppendLine(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null) errorBuilder.AppendLine(args.Data);
        };

        _logger.LogDebug("FFmpeg command: {Command}", process.StartInfo.Arguments);
        Console.WriteLine("⏳ Running FFmpeg (this may take a moment)...");

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Set timeout to 10 minutes
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));

        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(true);
            throw new TimeoutException("Audio extraction timed out after 10 minutes");
        }

        if (process.ExitCode != 0)
        {
            var errorOutput = errorBuilder.ToString();
            _logger.LogError("FFmpeg failed with exit code {ExitCode}. Error: {Error}", 
                process.ExitCode, errorOutput);
            throw new InvalidOperationException($"FFmpeg failed to extract audio. Exit code: {process.ExitCode}\n{errorOutput}");
        }

        if (!File.Exists(outputAudioPath))
        {
            throw new FileNotFoundException("Audio extraction did not produce the expected output file", outputAudioPath);
        }

        _logger.LogDebug("FFmpeg completed successfully. Audio file created at: {AudioPath}", outputAudioPath);
    }

    /// <summary>
    /// Cleanup temporary audio file
    /// </summary>
    public void CleanupAudioFile(string audioPath)
    {
        if (File.Exists(audioPath))
        {
            try
            {
                File.Delete(audioPath);
                _logger.LogDebug("Deleted temporary audio file: {AudioPath}", audioPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary audio file: {AudioPath}", audioPath);
            }
        }
    }
}
