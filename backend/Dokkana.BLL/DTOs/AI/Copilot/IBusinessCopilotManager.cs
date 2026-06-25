using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Copilot;

public interface IBusinessCopilotManager
{
    Task<BusinessCopilotResponseDto> AskAsync(
        BusinessCopilotRequestDto request,
        CancellationToken cancellationToken = default);
}