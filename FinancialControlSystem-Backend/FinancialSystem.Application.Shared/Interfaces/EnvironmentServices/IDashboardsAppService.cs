using FinancialSystem.Application.Shared.Dtos.Environment;

namespace FinancialSystem.Application.Shared.Interfaces.EnvironmentServices
{
    public interface IDashboardsAppService
    {
        Task<FinancialSummaryDto> GetFinancialSummary();
        Task<List<TopRecurringGoalsAchievedDto>> GetTheFourMostAchievedRecurringGoals();
        Task<UnexpectedExpensesAnalysisDto> GetUnexpectedExpensesAnalysis();
        Task<GoalsSummaryDto> GetGoalsSummary();
        Task<List<BalanceOverTimeDto>> GetBalanceOverTime(FilterForBalanceOverTimeDto input);
        Task<List<AchievementsDistributionDto>> GetAchievementDistributionByPeriod();
        Task<List<ProjectedBalanceDto>> GetProjectedBalanceEvolution(FiltersForBalanceProjectionDto input);
    }
}