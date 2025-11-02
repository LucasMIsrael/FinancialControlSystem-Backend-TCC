using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class DashboardsAppService : AppServiceBase, IDashboardsAppService
    {
        private TimeZoneInfo _tzBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        private readonly IGeneralRepository<Environments> _environmentsRepository;
        private readonly IGeneralRepository<Goals> _goalsRepository;
        private readonly IGeneralRepository<PlannedExpensesAndProfits> _plannedTransactionsRepository;
        private readonly IGeneralRepository<UnplannedExpensesAndProfits> _unplannedTransactionsRepository;
        private readonly ApiSettings _apiSettings;
        private readonly HttpClient _httpClient;

        public DashboardsAppService(IAppSession appSession,
                                    IGeneralRepository<Environments> environmentsRepository,
                                    IGeneralRepository<Goals> goalsRepository,
                                    IGeneralRepository<PlannedExpensesAndProfits> plannedTransactionsRepository,
                                    IGeneralRepository<UnplannedExpensesAndProfits> unplannedTransactionsRepository,
                                    IOptions<ApiSettings> apiOptions,
                                    HttpClient httpClient) : base(appSession)
        {
            _environmentsRepository = environmentsRepository;
            _goalsRepository = goalsRepository;
            _plannedTransactionsRepository = plannedTransactionsRepository;
            _unplannedTransactionsRepository = unplannedTransactionsRepository;
            _httpClient = httpClient;
            _apiSettings = apiOptions.Value;
        }

        //resumo financeiro
        #region GetFinancialSummary
        public async Task<FinancialSummaryDto> GetFinancialSummary()
        {
            var environment = await _environmentsRepository.GetByIdAsync((Guid)EnvironmentId);

            if (environment == null)
                throw new Exception("Ambiente não encontrado");

            var unplanned = await _unplannedTransactionsRepository
                                  .GetAll()
                                  .Where(x => x.EnvironmentId == EnvironmentId &&
                                             !x.IsDeleted)
                                  .ToListAsync();

            var planned = await _plannedTransactionsRepository
                                .GetAll()
                                .Where(x => x.EnvironmentId == EnvironmentId &&
                                            x.TransactionDate <= DateTime.UtcNow &&
                                           !x.IsDeleted)
                                .ToListAsync();

            double totalProfit = unplanned.Where(x => x.Type == FinancialRecordTypeEnum.Profit).Sum(x => x.Amount) +
                                 planned.Where(x => x.Type == FinancialRecordTypeEnum.Profit).Sum(x => x.Amount);

            double totalExpense = unplanned.Where(x => x.Type == FinancialRecordTypeEnum.Expense).Sum(x => x.Amount) +
                                  planned.Where(x => x.Type == FinancialRecordTypeEnum.Expense).Sum(x => x.Amount);

            double balance = environment.TotalBalance;
            double margin = totalProfit > 0 ? ((totalProfit - totalExpense) / totalProfit) * 100 : 0;

            return new FinancialSummaryDto
            {
                CurrentBalance = balance,
                TotalProfit = totalProfit,
                TotalExpense = totalExpense,
                ProfitMargin = $"{margin:F1}%",
                Level = environment.FinancialControlLevel
            };
        }
        #endregion

        //saldo ao longo do tempo
        #region GetBalanceOverTime
        public async Task<List<BalanceOverTimeDto>> GetBalanceOverTime(FilterForBalanceOverTimeDto input)
        {
            if (input.StartDate.Hour != 03 && input.EndDate.Hour != 03)
            {
                input.StartDate = TimeZoneInfo.ConvertTimeToUtc(input.StartDate, _tzBrasilia);
                input.EndDate = TimeZoneInfo.ConvertTimeToUtc(input.EndDate, _tzBrasilia);
            }

            var environment = await _environmentsRepository.GetByIdAsync((Guid)EnvironmentId);
            double runningBalance = environment?.TotalBalance ?? 0;

            var unplanned = await _unplannedTransactionsRepository
                                  .GetAll()
                                  .Where(x => x.EnvironmentId == EnvironmentId &&
                                             !x.IsDeleted /*&&
                                              x.TransactionDate.Date >= input.StartDate.Date &&
                                              x.TransactionDate.Date <= input.EndDate.Date*/)
                                  .ToListAsync();

            var planned = await _plannedTransactionsRepository
                                .GetAll()
                                .Where(x => x.EnvironmentId == EnvironmentId &&
                                           !x.IsDeleted /*&&
                                            x.TransactionDate >= input.StartDate &&
                                            x.TransactionDate <= input.EndDate*/)
                                .ToListAsync();

            //juntar e ordenar decrescente, do mais recente para o mais antigo
            var transactions = unplanned
                              .Select(x => new { x.TransactionDate, x.Type, x.Amount })
                              .Concat(planned.Select(x => new { x.TransactionDate, x.Type, x.Amount }))
                              .OrderByDescending(x => x.TransactionDate)
                              .ToList();

            var result = new List<BalanceOverTimeDto>();

            //construir saldo para trás
            foreach (var t in transactions)
            {
                //inverter o efeito da transação para voltar no tempo
                runningBalance -= (t.Type == FinancialRecordTypeEnum.Profit ? t.Amount : -t.Amount);

                result.Add(new BalanceOverTimeDto
                {
                    Date = t.TransactionDate,
                    Balance = runningBalance
                });
            }

            result = result.Where(x => x.Date.Date >= input.StartDate.Date &&
                                       x.Date.Date <= input.EndDate.Date).ToList();

            return result.OrderBy(x => x.Date).ToList();
        }
        #endregion

        //resumo de metas não recorrentes
        #region GetGoalsSummary
        public async Task<GoalsSummaryDto> GetGoalsSummary()
        {
            var goals = await _goalsRepository.GetAll()
                              .Where(x => x.EnvironmentId == EnvironmentId &&
                                         !x.IsDeleted && (
                                          x.PeriodType == null ||
                                          x.PeriodType == GoalPeriodTypeEnum.None) &&
                                          x.SingleDate != null &&
                                          x.SingleDate <= DateTime.UtcNow)
                              .ToListAsync();

            int completed = goals.Count(x => x.Status == true);
            int pending = goals.Count(x => x.Status != true);

            return new GoalsSummaryDto
            {
                Completed = completed,
                Pending = pending
            };
        }
        #endregion

        //analise de despesas não recorrentes
        #region GetUnexpectedExpensesAnalysis
        public async Task<UnexpectedExpensesAnalysisDto> GetUnexpectedExpensesAnalysis()
        {
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-30);

            var unplannedExpenses = await _unplannedTransactionsRepository
                                          .GetAll()
                                          .Where(x => x.EnvironmentId == EnvironmentId &&
                                                      x.Type == FinancialRecordTypeEnum.Expense &&
                                                      x.TransactionDate.Date >= startDate &&
                                                      x.TransactionDate.Date <= today &&
                                                     !x.IsDeleted)
                                          .ToListAsync();

            double totalUnexpectedExpenses = unplannedExpenses.Sum(x => x.Amount);

            // lucros (planejados + não planejados) nos últimos 30 dias
            var plannedProfits = await _plannedTransactionsRepository
                                       .GetAll()
                                       .Where(x => x.EnvironmentId == EnvironmentId &&
                                                   x.Type == FinancialRecordTypeEnum.Profit &&
                                                   x.TransactionDate.Date >= startDate &&
                                                   x.TransactionDate.Date <= today &&
                                                  !x.IsDeleted)
                                       .ToListAsync();

            var unplannedProfits = await _unplannedTransactionsRepository
                                         .GetAll()
                                         .Where(x => x.EnvironmentId == EnvironmentId &&
                                                     x.Type == FinancialRecordTypeEnum.Profit &&
                                                     x.TransactionDate.Date >= startDate &&
                                                     x.TransactionDate.Date <= today &&
                                                    !x.IsDeleted)
                                         .ToListAsync();

            double totalProfits = plannedProfits.Sum(x => x.Amount) + unplannedProfits.Sum(x => x.Amount);

            double percentage = totalProfits > 0 ?
                  (totalUnexpectedExpenses / totalProfits) * 100 : 0;

            string alertLevel;
            if (percentage < 15) alertLevel = "Baixo";
            else if (percentage < 30) alertLevel = "Moderado";
            else alertLevel = "Alto";

            return new UnexpectedExpensesAnalysisDto
            {
                TotalUnexpectedExpenses = totalUnexpectedExpenses,
                TotalProfits = totalProfits,
                Percentage = $"{percentage:F1}%",
                AlertLevel = alertLevel
            };
        }
        #endregion

        //as 10 metas recorrentes mais alcançadas
        #region GetTheFourMostAchievedRecurringGoals
        public async Task<List<TopRecurringGoalsAchievedDto>> GetTheFourMostAchievedRecurringGoals()
        {
            var goals = await _goalsRepository
                              .GetAll()
                              .Where(x => x.EnvironmentId == EnvironmentId &&
                                         !x.IsDeleted &&
                                          x.PeriodType != null &&
                                          x.PeriodType != GoalPeriodTypeEnum.None &&
                                          x.StartDate <= DateTime.UtcNow.Date)
                              .OrderByDescending(x => x.AchievementsCount).Take(10)
                              .ToListAsync();

            if (!goals.Any())
                return new List<TopRecurringGoalsAchievedDto>();

            var outputList = new List<TopRecurringGoalsAchievedDto>();

            goals.ForEach(goal =>
            {
                outputList.Add(new TopRecurringGoalsAchievedDto
                {
                    AchievementsCount = goal.AchievementsCount,
                    Description = goal.Description,
                    GoalNumber = goal.GoalNumber,
                    Value = goal.Value,
                });
            });

            return outputList;
        }
        #endregion

        //periodos de metas recorrentes mais alcançadas
        #region GetAchievementsDistribution
        public async Task<List<AchievementsDistributionDto>> GetAchievementDistributionByPeriod()
        {
            var environment = await _environmentsRepository.GetByIdAsync((Guid)EnvironmentId);
            if (environment == null)
                throw new Exception("Ambiente não encontrado");

            var groupedData = await _goalsRepository
                                    .GetAll()
                                    .Where(g => g.EnvironmentId == environment.Id &&
                                                g.PeriodType != null &&
                                                g.PeriodType != GoalPeriodTypeEnum.None)
                                    .GroupBy(g => g.PeriodType)
                                    .Select(g => new AchievementsDistributionDto
                                    {
                                        PeriodType = g.Key.ToString(),
                                        TotalAchievements = g.Sum(x => x.AchievementsCount)
                                    })
                                    .ToListAsync();

            return groupedData;
        }
        #endregion

        //simula futuro (projeção) do saldo total
        #region GetProjectedBalanceEvolution
        public async Task<List<ProjectedBalanceDto>> GetProjectedBalanceEvolution(FiltersForBalanceProjectionDto input)
        {
            var environment = await _environmentsRepository.GetByIdAsync((Guid)EnvironmentId);

            if (environment == null)
                throw new Exception("Ambiente não encontrado");

            var plannedTransactions = await _plannedTransactionsRepository
                                            .GetAll()
                                            .Where(x => x.EnvironmentId == EnvironmentId && !x.IsDeleted)
                                            .ToListAsync();

            DateTime startDate = DateTime.UtcNow;
            DateTime endDate = input.IsYear ?
                               startDate.AddYears(input.PeriodValue) :
                               startDate.AddMonths(input.PeriodValue);

            var projection = new List<ProjectedBalanceDto>();
            double runningBalance = environment.TotalBalance;

            if (input.IsYear)
            {
                projection.Add(new ProjectedBalanceDto
                {
                    PeriodLabel = startDate.Year.ToString() + " (Saldo Atual)",
                    ProjectedBalance = Math.Round(runningBalance, 2)
                });

                for (int i = 0; i < input.PeriodValue; i++)
                {
                    DateTime currentEnd = startDate.AddYears(1);
                    double balanceChange = CalculateProjectedChange(plannedTransactions, startDate, currentEnd);
                    runningBalance += balanceChange;

                    projection.Add(new ProjectedBalanceDto
                    {
                        PeriodLabel = currentEnd.Year.ToString(),
                        ProjectedBalance = Math.Round(runningBalance, 2)
                    });

                    startDate = currentEnd; //avançar para o próximo período
                }
            }
            else
            {
                projection.Add(new ProjectedBalanceDto
                {
                    PeriodLabel = $"{startDate:MMM/yyyy} (Saldo Atual)",
                    ProjectedBalance = Math.Round(runningBalance, 2)
                });

                for (int i = 0; i < input.PeriodValue; i++)
                {
                    DateTime currentEnd = startDate.AddMonths(1);
                    double balanceChange = CalculateProjectedChange(plannedTransactions, startDate, currentEnd);
                    runningBalance += balanceChange;

                    projection.Add(new ProjectedBalanceDto
                    {
                        PeriodLabel = $"{currentEnd:MMM/yyyy}",
                        ProjectedBalance = Math.Round(runningBalance, 2)
                    });

                    startDate = currentEnd; //avançar para o próximo mês
                }
            }

            return projection;
        }
        #endregion

        #region HelperMethods
        private double CalculateProjectedChange(List<PlannedExpensesAndProfits> transactions, DateTime from, DateTime to)
        {
            double totalChange = 0;

            foreach (var t in transactions)
            {
                if (t.TransactionDate > to)
                    continue;

                int occurrences = GetOccurrencesInRange(t, from, to);
                double totalValue = occurrences * t.Amount;

                totalChange += (t.Type == FinancialRecordTypeEnum.Profit) ? totalValue : -totalValue;
            }

            return totalChange;
        }

        private int GetOccurrencesInRange(PlannedExpensesAndProfits transaction, DateTime from, DateTime to)
        {
            if (transaction.TransactionDate > to)
                return 0;

            DateTime effectiveStart = transaction.TransactionDate > from ? transaction.TransactionDate : from;

            switch (transaction.RecurrenceType)
            {
                case RecurrenceTypeEnum.None:
                    return (transaction.TransactionDate >= from && transaction.TransactionDate <= to) ? 1 : 0;

                case RecurrenceTypeEnum.Daily:
                    return (to - effectiveStart).Days + 1;

                case RecurrenceTypeEnum.Weekly:
                    return ((to - effectiveStart).Days / 7) + 1;

                case RecurrenceTypeEnum.Monthly:
                    return ((to.Year - effectiveStart.Year) * 12 + (to.Month - effectiveStart.Month)) + 1;

                default:
                    return 0;
            }
        }
        #endregion

        #region EditTotalBalance
        public async Task EditTotalBalance(double value)
        {
            var environment = await _environmentsRepository.FirstOrDefaultAsync(x => x.Id == EnvironmentId);

            if (environment == null) throw new Exception("Ambiente não encontrado");

            environment.TotalBalance = value;
            await _environmentsRepository.UpdateAsync(environment);
        }
        #endregion

        #region GeminiConnection
        public async Task<string> GeminiConnection(string pergunta)
        {
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/" +
                      $"gemini-2.5-flash-preview-09-2025:generateContent?key={_apiSettings.Key}";

            var requestObj = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = pergunta }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsJsonAsync(url, requestObj);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Erro da API Gemini: {json}");

            var result = System.Text.Json.JsonSerializer.Deserialize<GeminiResponse>(
                json,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );

            return result.candidates[0].content.parts[0].text;
        }
        #endregion
    }
}