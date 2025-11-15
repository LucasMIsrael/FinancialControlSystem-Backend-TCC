using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers
{
    [Route("api/goalsManipulation")]
    [ApiController]
    public class GoalsManipulationController : Controller
    {
        private readonly IGoalsSettingsAppService _goalsSettingsAppService;
        private readonly ILogger<GoalsManipulationController> _logger;

        public GoalsManipulationController(IGoalsSettingsAppService goalsSettingsAppService,
                                           ILogger<GoalsManipulationController> logger)
        {
            _goalsSettingsAppService = goalsSettingsAppService;
            _logger = logger;
        }

        [HttpPost("create/goal")]
        [Authorize]
        public async Task<IActionResult> GoalCreationController(GoalDataDto input)
        {
            try
            {
                await _goalsSettingsAppService.InsertNewGoal(input);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao cadastrar nova Meta: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/goal")]
        [Authorize]
        public async Task<IActionResult> GoalUpdateController(GoalDataDto input)
        {
            try
            {
                await _goalsSettingsAppService.UpdateGoal(input);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao atualizar Meta: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/goal")]
        [Authorize]
        public async Task<IActionResult> GoalExclusionController(Guid id)
        {
            try
            {
                await _goalsSettingsAppService.DeleteGoal(id);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao excluir Meta: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/all/goals")]
        [Authorize]
        public async Task<IActionResult> GetAllGoalsInList()
        {
            try
            {
                return Ok(await _goalsSettingsAppService.GetAllGoals());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar lista de Metas: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/achieved/goals")]
        [Authorize]
        public async Task<IActionResult> AchievedGoalUpdateController()
        {
            try
            {
                await _goalsSettingsAppService.UpdateGoalsAchieved();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao atualizar Metas alcançadas: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}