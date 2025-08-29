using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers.EnvManipulation
{
    [Route("api/envManipulation")]
    [ApiController]
    public class EnvManipulationController : Controller
    {
        private readonly IEnvironmentSettingsAppService _environmentSettingsAppService;

        public EnvManipulationController(IEnvironmentSettingsAppService environmentSettingsAppService)
        {
            _environmentSettingsAppService = environmentSettingsAppService;
        }

        [HttpPost("create/environment")]
        [Authorize]
        public async Task<IActionResult> EnvCreationController(EnvironmentDataDto input)
        {
            try
            {
                await _environmentSettingsAppService.InsertEnvironment(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/environment")]
        [Authorize]
        public async Task<IActionResult> EnvUpdateController(EnvironmentDataDto input)
        {
            try
            {
                await _environmentSettingsAppService.UpdateEnvironment(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("delete/environment")]
        [Authorize]
        public async Task<IActionResult> EnvExclusionController(Guid input)
        {
            try
            {
                await _environmentSettingsAppService.DeleteEnvironment(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/all/environment")]
        [Authorize]
        public async Task<IActionResult> GetAllEnvironmentsInList()
        {
            try
            {
                return Ok(await _environmentSettingsAppService.GetAllEnvironments());
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get/environment")]
        [Authorize]
        public async Task<IActionResult> GetDataFromSpecificEnvironment(Guid input)
        {
            try
            {
                return Ok(await _environmentSettingsAppService.GetEnvironment(input));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}