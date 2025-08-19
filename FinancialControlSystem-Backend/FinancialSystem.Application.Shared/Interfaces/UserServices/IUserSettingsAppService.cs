using FinancialSystem.Application.Shared.Dtos.User;

namespace FinancialSystem.Application.Shared.Interfaces.UserServices
{
    public interface IUserSettingsAppService
    {
        Task RegisterUser(UserDataDto input);
        Task<string> UserLogin(UserDataDto input);
    }
}