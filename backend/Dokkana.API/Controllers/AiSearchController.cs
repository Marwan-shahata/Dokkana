using Dokkana.BLL.AI.Agent;
using Dokkana.BLL.DTOs.AI;
using Dokkana.Common;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiSearchController : ControllerBase
    {
        private readonly IDokkanaAgent _agent;

        public AiSearchController(IDokkanaAgent agent)
        {
            _agent = agent;
        }

        // Natural-language product search. The controller stays thin — all the AI
        // work happens inside the agent (BLL).
        [HttpPost("search")]
        public async Task<IActionResult> Search(AiSearchRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(GeneralResult.Fail("Query is required."));

            var result = await _agent.AskAsync(request);
            return Ok(GeneralResult<AiSearchResponseDTO>.Ok(result));
        }
    }
}
