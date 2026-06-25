using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public class AiSearchManager : IAiSearchManager
{
    private readonly IInputInterpreterAgent _inputInterpreterAgent;
    private readonly ILocalSearchAgent _localSearchAgent;
    private readonly IWebSearchAgent _webSearchAgent;
    private readonly IRankingAgent _rankingAgent;

    public AiSearchManager(
        IInputInterpreterAgent inputInterpreterAgent,
        ILocalSearchAgent localSearchAgent,
        IWebSearchAgent webSearchAgent,
        IRankingAgent rankingAgent)
    {
        _inputInterpreterAgent = inputInterpreterAgent;
        _localSearchAgent = localSearchAgent;
        _webSearchAgent = webSearchAgent;
        _rankingAgent = rankingAgent;
    }

    public async Task<AiSearchResponseDto> SearchAsync(
        AiSearchRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new AiSearchResponseDto
            {
                Query = request.Query,
                Answer = "اكتب المنتج اللي بتدور عليه."
            };
        }

        var intent = await _inputInterpreterAgent.InterpretAsync(
            request,
            cancellationToken);

        var localResults = await _localSearchAgent.SearchAsync(
            intent,
            request,
            cancellationToken);

        var webResults = request.IncludeWeb
            ? await _webSearchAgent.SearchAsync(intent, cancellationToken)
            : new List<AiWebResultDto>();

        var rankedResults = _rankingAgent.Rank(
            intent,
            localResults,
            webResults);

        localResults = localResults
            .OrderByDescending(x => x.Score)
            .ToList();

        webResults = webResults
            .OrderByDescending(x => x.Score)
            .ToList();

        return new AiSearchResponseDto
        {
            Query = request.Query,
            Intent = intent,
            LocalResults = localResults,
            WebResults = webResults,
            RankedResults = rankedResults,
            Answer = BuildAnswer(localResults, webResults, rankedResults)
        };
    }

    private static string BuildAnswer(
        List<AiProductResultDto> localResults,
        List<AiWebResultDto> webResults,
        List<AiRankedResultDto> rankedResults)
    {
        if (!rankedResults.Any())
        {
            return "مش لاقي نتائج مناسبة حاليًا، لا من المحلات المحلية ولا من البحث على الإنترنت.";
        }

        if (localResults.Any() && webResults.Any())
        {
            return $"لقيت {localResults.Count} منتجات من المحلات المحلية و {webResults.Count} نتائج من الإنترنت، ورتبتهم حسب الأقرب للبحث.";
        }

        if (localResults.Any())
        {
            return $"لقيت {localResults.Count} منتجات من المحلات المحلية ورتبتهم حسب الأقرب للبحث.";
        }

        return $"مش لاقي منتجات محلية مناسبة حاليًا، لكن لقيت {webResults.Count} نتائج من الإنترنت ممكن تساعدك.";
    }
}
