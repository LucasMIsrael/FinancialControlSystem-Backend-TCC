using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces.UserSettings;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace FinancialSystem.Web.Controllers.LoginAndRegister
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginAndRegisterController : Controller
    {
        private readonly IUserSettingsAppService _userSettingsAppService;

        public LoginAndRegisterController(IUserSettingsAppService userSettingsAppService)
        {
            _userSettingsAppService = userSettingsAppService;
        }

    }
}
