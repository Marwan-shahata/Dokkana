using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public interface IRankingAgent
{
    List<AiRankedResultDto> Rank(
        AiSearchIntentDto intent,
        List<AiProductResultDto> localResults,
        List<AiWebResultDto> webResults);
}