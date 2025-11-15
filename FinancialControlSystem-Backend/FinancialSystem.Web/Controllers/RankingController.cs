using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers
{
    [Route("api/ranking")]
    [ApiController]
    public class RankingController : Controller
    {
        private readonly IRankingAppService _rankingAppService;
        private readonly ILogger<RankingController> _logger;

        public RankingController(IRankingAppService rankingAppService,
                                 ILogger<RankingController> logger)
        {
            _rankingAppService = rankingAppService;
            _logger = logger;
        }

        [HttpGet("get")]
        [Authorize]
        public async Task<IActionResult> GetRanking()
        {
            try
            {
                return Ok(await _rankingAppService.GetEnvironmentsForRanking());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar ranking de ambientes: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
