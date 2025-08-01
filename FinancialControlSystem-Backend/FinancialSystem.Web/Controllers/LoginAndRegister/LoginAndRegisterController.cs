using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces.UserSettings;
using Microsoft.AspNetCore.Mvc;

namespace FinancialSystem.Web.Controllers.LoginAndRegister
{
    [Route("api/loginAndRegister")]
    [ApiController]    
    public class LoginAndRegisterController : Controller
    {
        private readonly IUserSettingsAppService _userSettingsAppService;

        public LoginAndRegisterController(IUserSettingsAppService userSettingsAppService)
        {
            _userSettingsAppService = userSettingsAppService;
        }

        [HttpPost("register/user")]
        public async Task<IActionResult> UserRegistrationController(UserDataDto input)
        {
            try
            {
                await _userSettingsAppService.RegisterUser(input);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login/user")]
        public async Task<IActionResult> UserLoginController(UserDataDto input)
        {
            try
            {
                var token = await _userSettingsAppService.UserLogin(input);
                return Ok(new { token });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
