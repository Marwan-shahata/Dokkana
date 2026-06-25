using Dokkana.BLL.AI.Agents;
using Dokkana.BLL.AI.Catalog;
using Dokkana.BLL.AI.Copilot;
using Dokkana.BLL.DTOs.AI;
using Dokkana.BLL.DTOs.AI.Agent;
using Dokkana.BLL.DTOs.AI.Catalog;
using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Orchestrator;

public class DokkanaAgentManager : IDokkanaAgentManager
{
    private readonly IRouteClassifierAgent _routeClassifierAgent;
    private readonly IAiSearchManager _aiSearchManager;
    private readonly ICatalogRagManager _catalogRagManager;
    private readonly IBusinessCopilotManager _businessCopilotManager;

    public DokkanaAgentManager(
        IRouteClassifierAgent routeClassifierAgent,
        IAiSearchManager aiSearchManager,
        ICatalogRagManager catalogRagManager,
        IBusinessCopilotManager businessCopilotManager)
    {
        _routeClassifierAgent = routeClassifierAgent;
        _aiSearchManager = aiSearchManager;
        _catalogRagManager = catalogRagManager;
        _businessCopilotManager = businessCopilotManager;
    }

    public async Task<DokkanaAgentResponseDto> AskAsync(
        DokkanaAgentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return new DokkanaAgentResponseDto
            {
                Message = request.Message,
                Route = new AgentRouteDto
                {
                    Route = "general",
                    Confidence = 1,
                    Reason = "Empty message"
                },
                Answer = "اكتب سؤالك أو طلبك."
            };
        }

        var route = await _routeClassifierAgent.ClassifyAsync(
            request,
            cancellationToken);

        return route.Route switch
        {
            "product_search" => await HandleProductSearchAsync(
                request,
                route,
                cancellationToken),

            "catalog_rag" => await HandleCatalogRagAsync(
                request,
                route,
                cancellationToken),

            "business_copilot" => await HandleBusinessCopilotAsync(
                request,
                route,
                cancellationToken),

            _ => HandleGeneral(request, route)
        };
    }

    private async Task<DokkanaAgentResponseDto> HandleProductSearchAsync(
        DokkanaAgentRequestDto request,
        AgentRouteDto route,
        CancellationToken cancellationToken)
    {
        var searchRequest = new AiSearchRequestDto
        {
            Query = request.Message,
            IncludeWeb = request.IncludeWeb,
            PageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber,
            PageSize = request.PageSize <= 0 ? 10 : request.PageSize,
            UserLat = request.UserLat,
            UserLng = request.UserLng
        };

        var searchResult = await _aiSearchManager.SearchAsync(
            searchRequest,
            cancellationToken);

        return new DokkanaAgentResponseDto
        {
            Message = request.Message,
            Route = route,
            Answer = searchResult.Answer,
            ProductSearch = searchResult
        };
    }

    private async Task<DokkanaAgentResponseDto> HandleCatalogRagAsync(
        DokkanaAgentRequestDto request,
        AgentRouteDto route,
        CancellationToken cancellationToken)
    {
        if (!request.StoreId.HasValue || request.StoreId.Value <= 0)
        {
            return new DokkanaAgentResponseDto
            {
                Message = request.Message,
                Route = route,
                Answer = "السؤال ده محتاج StoreId عشان أقدر أبحث داخل كتالوج محل معين."
            };
        }

        var catalogRequest = new CatalogQuestionRequestDto
        {
            StoreId = request.StoreId.Value,
            Question = request.Message,
            MaxChunks = request.MaxChunks <= 0 ? 5 : request.MaxChunks
        };

        var catalogResult = await _catalogRagManager.AskAsync(
            catalogRequest,
            cancellationToken);

        return new DokkanaAgentResponseDto
        {
            Message = request.Message,
            Route = route,
            Answer = catalogResult.Answer,
            Catalog = catalogResult
        };
    }

    private async Task<DokkanaAgentResponseDto> HandleBusinessCopilotAsync(
        DokkanaAgentRequestDto request,
        AgentRouteDto route,
        CancellationToken cancellationToken)
    {
        if (!request.StoreId.HasValue || request.StoreId.Value <= 0)
        {
            return new DokkanaAgentResponseDto
            {
                Message = request.Message,
                Route = route,
                Answer = "السؤال ده محتاج StoreId عشان أقدر أجيب بيانات المبيعات أو المخزون للمحل."
            };
        }

        var copilotRequest = new BusinessCopilotRequestDto
        {
            StoreId = request.StoreId.Value,
            Question = request.Message
        };

        var copilotResult = await _businessCopilotManager.AskAsync(
            copilotRequest,
            cancellationToken);

        return new DokkanaAgentResponseDto
        {
            Message = request.Message,
            Route = route,
            Answer = copilotResult.Answer,
            Copilot = copilotResult
        };
    }

    private static DokkanaAgentResponseDto HandleGeneral(
        DokkanaAgentRequestDto request,
        AgentRouteDto route)
    {
        return new DokkanaAgentResponseDto
        {
            Message = request.Message,
            Route = route,
            Answer = "أقدر أساعدك في البحث عن منتجات، سؤال على كتالوج محل، أو تحليل مبيعات ومخزون المحل."
        };
    }
}