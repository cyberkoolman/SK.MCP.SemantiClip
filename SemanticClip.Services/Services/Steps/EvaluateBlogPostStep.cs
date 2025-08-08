using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using SemanticClip.Core.Models;
using SemanticClip.Services.Utils;
using Microsoft.SemanticKernel.Agents.AzureAI;
using Microsoft.SemanticKernel.ChatCompletion;
using SemanticClip.Services.Utilities;
// GA: Use the Persistent namespace for the GA Foundry Agent SDK
using Azure.AI.Agents.Persistent;
using AgentThread = Microsoft.SemanticKernel.Agents.AgentThread;

namespace SemanticClip.Services.Steps;

public class EvaluateBlogPostStep : KernelProcessStep<VideoProcessingResponse>
{
    private VideoProcessingResponse? _state;
    private readonly ILogger<EvaluateBlogPostStep> _logger;
    public EvaluateBlogPostStep(ILogger<EvaluateBlogPostStep> logger)
    {
        _logger = logger;
    }

    public override ValueTask ActivateAsync(KernelProcessStepState<VideoProcessingResponse> state)
    {
        _state = state.State;
        return ValueTask.CompletedTask;
    }

    // Refactored to use PersistentAgentsClient (GA version)

    private async Task<AzureAIAgent> UseTemplateForAzureAIAgentAsync(
        PersistentAgentsClient agentsClient, string blogPost)
    {
        string yaml = EmbeddedResource.Read("EvaluateBlogPost.yaml");
        var templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(yaml);
        var templateFactory = new KernelPromptTemplateFactory();

        var response = await agentsClient.Administration.CreateAgentAsync(
            model: AzureAIAgentConfig.ChatModelId,
            name: templateConfig.Name,
            instructions: templateConfig.Template,
            description: templateConfig.Description
        );

        PersistentAgent agentDef = response.Value;

        AzureAIAgent agent = new(
            agentDef,
            agentsClient,
            templateFactory: templateFactory,
            templateFormat: PromptTemplateConfig.SemanticKernelTemplateFormat)
        {
            Arguments = new KernelArguments
            {
                { "blogPost", blogPost }
            }
        };     

        return agent;
    }
    public static class Functions
    {
        public const string EvaluateBlogPost = nameof(EvaluateBlogPost);
    }

    [KernelFunction(Functions.EvaluateBlogPost)]
    public async Task EvaluateBlogPostAsync(BlogPostProcessingResponse blogstate, Kernel kernel, KernelProcessStepContext context)
    {
        _logger.LogInformation("Starting blog post evaluation process");
        BlogPostProcessingResponse _blogstate = blogstate;

        // GA: Create the PersistentAgentsClient for the GA workspace/project
        // var client = AzureAIAgent.CreateAzureAIClient(AzureAIAgentConfig.ConnectionString, new AzureCliCredential());
        // PersistentAgentsClient agentsClient = client.GetPersistentAgentsClient();
        var agentsClient = new PersistentAgentsClient(
            AzureAIAgentConfig.Endpoint, // Add this config field!
            new AzureCliCredential()
        );        

        var agent = await UseTemplateForAzureAIAgentAsync(
            agentsClient: agentsClient,
            blogPost: _blogstate!.BlogPosts[_blogstate.UpdateIndex]);

        // Create the chat history thread
        AgentThread thread = new AzureAIAgentThread(agentsClient);

        // Invoke the agent with the blog post
        try
        {
            var evaluation = await InvokeAgentAsync(agent, thread, _blogstate.BlogPosts[_blogstate.UpdateIndex]);

            // Create a VideoProcessingResponse object for markdown
            var evalResponse = new VideoProcessingResponse
            {
                BlogPost = evaluation,
                Transcript = _blogstate.VideoProcessingResponse.Transcript
            };

            await MarkdownFileHelper.SaveMarkdownFileAsync(
                evalResponse,
                GenerateBlogPostStep.FilePathStorage.CurrentFilePath, // Or pass the relevant file path
                "Evaluated",
                _logger);

            this._state!.BlogPost = evaluation;
            this._state.Transcript = _blogstate.VideoProcessingResponse.Transcript;
            string EvaluateBlogPostComplete = nameof(EvaluateBlogPostComplete);
            await context.EmitEventAsync(new() { Id = EvaluateBlogPostComplete, Data = this._state, Visibility = KernelProcessEventVisibility.Public });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error evaluating blog post: {Error}", ex.Message);
            throw;
        }
    }

    private async Task<string> InvokeAgentAsync(AzureAIAgent agent, AgentThread thread, string input)
    {
        var message = new ChatMessageContent(AuthorRole.User, input);
        string? lastResponse = null;

        await foreach (var response in agent.InvokeAsync(message, thread))
        {
            lastResponse = response.Message.Content;
            _logger.LogInformation("Agent response: {Response}", lastResponse);
        }

        return lastResponse ?? string.Empty;
    }
}
