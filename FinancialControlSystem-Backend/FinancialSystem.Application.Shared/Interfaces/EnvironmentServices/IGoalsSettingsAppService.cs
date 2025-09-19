using FinancialSystem.Application.Shared.Dtos.Environment;

namespace FinancialSystem.Application.Shared.Interfaces.EnvironmentServices
{
    public interface IGoalsSettingsAppService
    {
        Task InsertNewGoal(GoalDataDto input);
        Task UpdateGoal(GoalDataDto input);
        Task<List<GoalDataForViewDto>> GetAllGoals();
        Task DeleteGoal(Guid goalId);
    }
}