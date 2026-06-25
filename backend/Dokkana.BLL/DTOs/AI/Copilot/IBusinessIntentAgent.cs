using Dokkana.BLL.DTOs.AI.Copilot;

namespace Dokkana.BLL.AI.Copilot;

public interface IBusinessIntentAgent
{
    Task<BusinessIntentDto> DetectIntentAsync(
        string question,
        CancellationToken cancellationToken = default);
}