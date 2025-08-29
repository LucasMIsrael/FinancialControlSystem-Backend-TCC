using FinancialSystem.Application.Shared.Dtos.Environment;

namespace FinancialSystem.Application.Shared.Interfaces.EnvironmentServices
{
    public interface IEnvironmentSettingsAppService
    {
        Task InsertEnvironment(EnvironmentDataDto input);
        Task UpdateEnvironment(EnvironmentDataDto input);
        Task DeleteEnvironment(Guid envId);
        Task<List<EnvironmentDataDto>> GetAllEnvironments();
        Task<EnvironmentDataDto> GetEnvironment(Guid envId);
    }
}
