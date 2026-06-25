
using Dokkana.BLL.DTOs.AI;

namespace Dokkana.BLL.AI.Agents;

public interface IInputInterpreterAgent
{
    Task<AiSearchIntentDto> InterpretAsync(
        AiSearchRequestDto request,
        CancellationToken cancellationToken = default);
}