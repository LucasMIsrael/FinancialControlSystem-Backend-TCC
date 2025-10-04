using FinancialSystem.Application.Shared.Dtos.Environment;

namespace FinancialSystem.Application.Shared.Interfaces.EnvironmentServices
{
    public interface IRankingAppService
    {
        Task<List<RankingDto>> GetEnvironmentsForRanking();
    }
}
