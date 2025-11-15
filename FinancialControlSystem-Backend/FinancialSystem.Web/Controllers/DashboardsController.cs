using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers
{
    [Route("api/dashboard")]
    [ApiController]
    public class DashboardsController : Controller
    {
        private readonly IDashboardsAppService _dashAppService;
        private readonly ILogger<DashboardsController> _logger;

        public DashboardsController(IDashboardsAppService dashAppService,
                                    ILogger<DashboardsController> logger)
        {
            _dashAppService = dashAppService;
            _logger = logger;
        }

        [HttpGet("get/financial-summary")]
        [Authorize]
        public async Task<IActionResult> GetDataForFinancialSummary()
        {
            try
            {
                return Ok(await _dashAppService.GetFinancialSummary());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar dados para sumário financeiro: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/top-goals-achieved")]
        [Authorize]
        public async Task<IActionResult> GetTheMostAchievedRecurringGoals()
        {
            try
            {
                return Ok(await _dashAppService.GetTheFourMostAchievedRecurringGoals());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar as metas mais alcançadas: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/unplanned-expenses-analysis")]
        [Authorize]
        public async Task<IActionResult> GetUnplannedExpensesForAnalysis()
        {
            try
            {
                return Ok(await _dashAppService.GetUnexpectedExpensesAnalysis());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao efetuar análise de despesas inesperadas: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/non-recurring-goals")]
        [Authorize]
        public async Task<IActionResult> GetSummaryOfNonRecurringGoals()
        {
            try
            {
                return Ok(await _dashAppService.GetGoalsSummary());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar dados de metas não recorrentes: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/balance-over-time")]
        [Authorize]
        public async Task<IActionResult> GetTotalBalanceOverTime([FromQuery] FilterForBalanceOverTimeDto filter)
        {
            try
            {
                return Ok(await _dashAppService.GetBalanceOverTime(filter));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar dados para sumário financeiro: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/goals-distribuition")]
        [Authorize]
        public async Task<IActionResult> GetPeriodsOfMoreAchievedGoals()
        {
            try
            {
                return Ok(await _dashAppService.GetAchievementDistributionByPeriod());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar períodos com metas mais alcançadas: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/balance-projection")]
        [Authorize]
        public async Task<IActionResult> GetTheTotalBalanceProjection([FromQuery] FiltersForBalanceProjectionDto filter)
        {
            try
            {
                if (filter.IsYear && filter.PeriodValue > 10)
                    throw new Exception("Limite máximo de até 10 anos");
                else if (!filter.IsYear && filter.PeriodValue > 12)
                    throw new Exception("Limite máximo de até 12 meses");

                return Ok(await _dashAppService.GetProjectedBalanceEvolution(filter));
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao efetuar projeção de saldo total: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("edit/envBalance")]
        [Authorize]
        public async Task<IActionResult> UpdateEnvironmentTotalBalance([FromBody] EditTotalBalanceDto value)
        {
            try
            {
                await _dashAppService.EditTotalBalance(value.Value);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao atualizar saldo total: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("ai/communicate")]
        [Authorize]
        public async Task<IActionResult> CommunicationWithAI([FromBody] PromptRequestDto request)
        {
            try
            {
                string response = await _dashAppService.GeminiConnection(request.Prompt);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao efetuar comunicação com IA: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}