using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class GoalsSettingsAppService : AppServiceBase, IGoalsSettingsAppService
    {
        private readonly IGeneralRepository<Goals> _goalsRepository;
        private TimeZoneInfo _tzBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        public GoalsSettingsAppService(IAppSession appSession,
                                       IGeneralRepository<Goals> goalsRepository) : base(appSession)
        {
            _goalsRepository = goalsRepository;
        }

        #region InsertNewGoal
        public async Task InsertNewGoal(GoalDataDto input)
        {
            var lastGoalNumber = _goalsRepository.GetAll()
                                                 .Where(x => !x.IsDeleted &&
                                                              x.EnvironmentId == (Guid)EnvironmentId)
                                                       .OrderByDescending(x => x.CreationTime)
                                                       .Select(x => x.GoalNumber)
                                                       .FirstOrDefault();
            var goal = new Goals
            {
                Id = Guid.NewGuid(),
                EnvironmentId = (Guid)EnvironmentId,
                GoalNumber = (lastGoalNumber == 0 ? 1 : lastGoalNumber + 1),
                Description = input.Description,
                PeriodType = input.PeriodType,
                Value = input.Value,
                Status = false,
                SingleDate = input.SingleDate.HasValue ?
                             TimeZoneInfo.ConvertTimeToUtc(input.SingleDate.Value, _tzBrasilia) :
                             null,
                StartDate = input.StartDate.HasValue ?
                            TimeZoneInfo.ConvertTimeToUtc(input.StartDate.Value, _tzBrasilia) :
                            null,
            };

            await _goalsRepository.InsertAsync(goal);
        }
        #endregion

        #region UpdateGoal
        public async Task UpdateGoal(GoalDataDto input)
        {
            var goal = await _goalsRepository.FirstOrDefaultAsync(x => x.Id == (Guid)input.Id);

            if (goal == null)
                throw new Exception("Meta não encontrada");

            goal.Description = input.Description;
            goal.SingleDate = input.SingleDate.HasValue ?
                              TimeZoneInfo.ConvertTimeToUtc(input.SingleDate.Value, _tzBrasilia) :
                              null;
            goal.StartDate = input.StartDate.HasValue ?
                             TimeZoneInfo.ConvertTimeToUtc(input.StartDate.Value, _tzBrasilia) :
                             null;
            goal.Value = input.Value;
            goal.PeriodType = input.PeriodType;

            await _goalsRepository.UpdateAsync(goal);
        }
        #endregion

        #region GetAllGoals
        public async Task<List<GoalDataForViewDto>> GetAllGoals()
        {
            var goalsQuery = await _goalsRepository.GetAll()
                                                   .Where(x => x.EnvironmentId == (Guid)EnvironmentId &&
                                                              !x.IsDeleted)
                                                   .ToListAsync();
            if (goalsQuery.Count == 0)
                return new List<GoalDataForViewDto>();

            var outputList = new List<GoalDataForViewDto>();

            goalsQuery.ForEach(goals =>
            {
                outputList.Add(new GoalDataForViewDto
                {
                    Description = goals.Description,
                    GoalNumber = goals.GoalNumber,
                    Id = goals.Id,
                    PeriodType = goals.PeriodType,
                    Status = goals.Status,
                    Value = goals.Value,
                    SingleDate = goals.SingleDate.HasValue ?
                                 goals.SingleDate.Value.AddHours(-3) :
                                 null,
                    StartDate = goals.StartDate.HasValue ?
                                goals.StartDate.Value.AddHours(-3) :
                                null
                });
            });

            return outputList;
        }
        #endregion

        #region DeleteGoal
        public async Task DeleteGoal(Guid goalId)
        {
            var goal = await _goalsRepository.GetByIdAsync(goalId);

            if (goal == null)
                throw new Exception("Meta não encontrada para exclusão");

            await _goalsRepository.DeleteAsync(goal);
        }
        #endregion

        #region UpdateGoalsAchieved
        public async Task UpdateGoalsAchieved()
        {
            var goals = await _goalsRepository
                              .GetAll().Include(g => g.Environment)
                              .Where(g => g.EnvironmentId == EnvironmentId &&
                                          g.Status == false)
                              .ToListAsync();

            var today = DateTime.Today;

            foreach (var goal in goals)
            {
                bool achieved = false;

                //caso seja meta recorrente
                if (goal.PeriodType.HasValue && goal.StartDate.HasValue)
                {
                    switch (goal.PeriodType.Value)
                    {
                        case GoalPeriodTypeEnum.Daily:
                            if (today >= goal.StartDate.Value.Date &&
                                goal.Environment.TotalBalance >= goal.Value)
                                achieved = true;
                            break;

                        case GoalPeriodTypeEnum.Weekly:
                            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
                            var currentWeek = cal.GetWeekOfYear(today, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);
                            var goalWeek = cal.GetWeekOfYear(goal.StartDate.Value, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Monday);

                            if (currentWeek == goalWeek &&
                                today.Year == goal.StartDate.Value.Year &&
                                goal.Environment.TotalBalance >= goal.Value)
                                achieved = true;
                            break;

                        case GoalPeriodTypeEnum.Monthly:
                            if (today.Month == goal.StartDate.Value.Month &&
                                today.Year == goal.StartDate.Value.Year &&
                                goal.Environment.TotalBalance >= goal.Value)
                                achieved = true;
                            break;

                        case GoalPeriodTypeEnum.Semestral:
                            int currentSemester = (today.Month - 1) / 6 + 1;
                            int goalSemester = (goal.StartDate.Value.Month - 1) / 6 + 1;

                            if (currentSemester == goalSemester &&
                                today.Year == goal.StartDate.Value.Year &&
                                goal.Environment.TotalBalance >= goal.Value)
                                achieved = true;
                            break;

                        case GoalPeriodTypeEnum.Annual:
                            if (today.Year == goal.StartDate.Value.Year &&
                                goal.Environment.TotalBalance >= goal.Value)
                                achieved = true;
                            break;
                    }
                }
                else if (goal.SingleDate.HasValue) //caso seja meta unica
                {
                    if (today <= goal.SingleDate.Value &&
                        goal.Environment.TotalBalance >= goal.Value)
                        achieved = true;
                }

                //atualiza status e contabiliza
                if (achieved && goal.Status != true)
                {
                    goal.Status = true;
                    goal.Environment.TotalGoalsAchieved++;

                    TotalGoalsAchievedValidation(goal);

                    await _goalsRepository.UpdateAsync(goal);
                }
            }
        }
        #endregion

        #region TotalGoalsAchievedValidation
        private static void TotalGoalsAchievedValidation(Goals goal)
        {
            switch (goal.Environment.TotalGoalsAchieved)
            {
                case 3:
                    goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.Beginner;
                    break;
                case 8:
                    goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.Learning;
                    break;
                case 15:
                    goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.Intermediate;
                    break;
                case 25:
                    goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.Advanced;
                    break;
                case 40:
                    goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.Expert;
                    break;
                case 60:
                    goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.Master;
                    break;
            }

            if (goal.Environment.TotalGoalsAchieved > 60)
                goal.Environment.FinancialControlLevel = FinancialControlLevelEnum.FinancialController;
        }
        #endregion
    }
}