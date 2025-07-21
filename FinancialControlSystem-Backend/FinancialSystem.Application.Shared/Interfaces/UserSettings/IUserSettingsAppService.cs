using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Core.Entities;

namespace FinancialSystem.Application.Shared.Interfaces.UserSettings
{
    public interface IUserSettingsAppService
    {
        Task RegisterAsync(UserDataDto input);
        Task<User?> LoginAsync(string email, string password);
    }
}