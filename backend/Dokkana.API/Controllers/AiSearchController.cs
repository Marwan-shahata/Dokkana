using Dokkana.BLL.AI;
using Dokkana.BLL.AI.Agent;
using Dokkana.BLL.DTOs.AI;
using Dokkana.Common;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace Dokkana.API.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiSearchController : ControllerBase
    {
        private const int MaxQueryLength = 300;

        private readonly IDokkanaAgent _agent;
        private readonly IAiProductMatcher _matcher;

        public AiSearchController(IDokkanaAgent agent, IAiProductMatcher matcher)
        {
            _agent = agent;
            _matcher = matcher;
        }

        // Natural-language product search. The agent produces the answer text (real
        // LLM when configured, otherwise a clear fallback message); the matcher
        // always returns real product cards from the catalog so the UI is useful
        // regardless of AI configuration.
        [HttpPost("search")]
        [EnableRateLimiting("ai")]
        public async Task<IActionResult> Search(AiSearchRequestDTO request)
        {
            if (string.IsNullOrWhiteSpace(request.Query))
                return BadRequest(GeneralResult.Fail("Query is required."));

            // Basic prompt guardrail: cap length and strip control characters
            // before the text reaches the LLM or the matcher.
            request.Query = Sanitize(request.Query);

            var result = await _agent.AskAsync(request);

            var (products, total) = await _matcher.MatchAsync(request.Query, request.StoreId);
            result.Products = products;
            result.TotalMatches = total;

            return Ok(GeneralResult<AiSearchResponseDTO>.Ok(result));
        }

        private static string Sanitize(string query)
        {
            var trimmed = query.Trim();
            if (trimmed.Length > MaxQueryLength)
                trimmed = trimmed[..MaxQueryLength];
            // Drop control chars (defensive against prompt-injection via newlines/escapes).
            return new string(trimmed.Where(c => !char.IsControl(c)).ToArray());
        }
    }
}
