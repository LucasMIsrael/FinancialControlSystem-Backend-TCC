using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces.UserSettings;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;

namespace FinancialSystem.Application.Services.UserSettings
{
    public class UserSettingsAppService : IUserSettingsAppService
    {
        private readonly IGeneralRepository<User> _userRepository;

        public UserSettingsAppService(IGeneralRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

    }
}
