using FinancialSystem.Core.Enums;

namespace FinancialSystem.Application.Shared.Dtos.Environment
{
    public class FinancialSummaryDto
    {
        public double CurrentBalance { get; set; }
        public double TotalProfit { get; set; }
        public double TotalExpense { get; set; }
        public string ProfitMargin { get; set; }
        public FinancialControlLevelEnum Level { get; set; }
    }

    public class BalanceOverTimeDto
    {
        public DateTime Date { get; set; }
        public double Balance { get; set; }
    }

    public class FilterForBalanceOverTimeDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class GoalsSummaryDto
    {
        public int Completed { get; set; }   // metas únicas batidas
        public int Pending { get; set; }     // metas únicas não batidas
    }

    public class UnexpectedExpensesAnalysisDto
    {
        public double TotalUnexpectedExpenses { get; set; }
        public double TotalProfits { get; set; }
        public string Percentage { get; set; }
        public string AlertLevel { get; set; }  // "Baixo", "Moderado", "Alto"
    }

    public class TopRecurringGoalsAchievedDto
    {
        public int AchievementsCount { get; set; }
        public int GoalNumber { get; set; }
        public string Description { get; set; }
        public double Value { get; set; }
    }

    public class AchievementsDistributionDto
    {
        public string PeriodType { get; set; }
        public int TotalAchievements { get; set; }
    }

    public class FiltersForBalanceProjectionDto
    {
        public int PeriodValue { get; set; }
        public bool IsYear { get; set; } = false;
    }

    public class ProjectedBalanceDto
    {
        public string PeriodLabel { get; set; }
        public double ProjectedBalance { get; set; }
    }

    public class EditTotalBalanceDto
    {
        public double Value { get; set; }
    }
}