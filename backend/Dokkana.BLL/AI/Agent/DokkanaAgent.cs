using Dokkana.BLL.AI.Prompts;
using Dokkana.BLL.DTOs.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Dokkana.BLL.AI.Agent
{
    public class DokkanaAgent : IDokkanaAgent
    {
        private readonly Kernel _kernel;
        private readonly IChatCompletionService _chatCompletionService;

        public DokkanaAgent(
            Kernel kernel,
            IChatCompletionService chatCompletionService)
        {
            _kernel = kernel;
            _chatCompletionService = chatCompletionService;
        }

        public async Task<AiSearchResponseDTO> AskAsync(
            AiSearchRequestDTO request,
            CancellationToken cancellationToken = default)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
            {
                return new AiSearchResponseDTO
                {
                    Query = request?.Query ?? string.Empty,
                    Answer = "من فضلك اكتب طلب البحث."
                };
            }

            var chatHistory = new ChatHistory();

            chatHistory.AddSystemMessage(AgentPrompts.ShoppingAssistant);
            chatHistory.AddUserMessage(request.Query);

            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Required()
            };

            var result = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory: chatHistory,
                executionSettings: executionSettings,
                kernel: _kernel,
                cancellationToken: cancellationToken);

            return new AiSearchResponseDTO
            {
                Query = request.Query,
                Answer = result.Content ?? "لم أستطع تكوين رد مناسب."
            };
        }
    }
}