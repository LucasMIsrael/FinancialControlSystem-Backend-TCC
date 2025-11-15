using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces.UserServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers
{
    [Route("api/userManipulation")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserSettingsAppService _userSettingsAppService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserSettingsAppService userSettingsAppService,
                              ILogger<UserController> logger)
        {
            _userSettingsAppService = userSettingsAppService;
            _logger = logger;
        }

        [HttpGet("get/user")]
        [Authorize]
        public async Task<IActionResult> GetUserData()
        {
            try
            {
                return Ok(await _userSettingsAppService.GetUserInformations());
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao buscar dados do usuário: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("update/user")]
        [Authorize]
        public async Task<IActionResult> UserUpdateController(UserDataForUpdateDto input)
        {
            try
            {
                await _userSettingsAppService.UpdateUserInformations(input);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"ERRO ao atualizar dados do usuário: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }
    }
}
