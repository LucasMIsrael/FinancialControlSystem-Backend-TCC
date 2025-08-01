using FinancialSystem.Application.Shared.Dtos.User;

namespace FinancialSystem.Application.Shared.Interfaces.UserSettings
{
    public interface IUserSettingsAppService
    {
        Task RegisterUser(UserDataDto input);
        Task<string> UserLogin(UserDataDto input);
    }
}