using FinancialSystem.Application.Shared.Dtos.Environment;

namespace FinancialSystem.Application.Shared.Interfaces.EnvironmentServices
{
    public interface IEnvironmentSettingsAppService
    {
        Task InsertOrUpdateEnvironment(EnvironmentDataDto input);
        Task DeleteEnvironment(Guid envId);
        Task<List<EnvironmentDataDto>> GetAllEnvironments();
        Task<EnvironmentDataDto> GetEnvironment(Guid envId);
    }
}
