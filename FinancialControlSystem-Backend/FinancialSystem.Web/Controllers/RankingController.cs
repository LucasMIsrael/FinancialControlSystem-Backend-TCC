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

        public RankingController(IRankingAppService rankingAppService)
        {
            _rankingAppService = rankingAppService;
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
                return BadRequest(ex.Message);
            }
        }
    }
}
