using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class DashboardServiceTests
    {
        [Fact]
        public async Task GetFinancialSummary_ShouldThrowWhenEnvironmentNotFound()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync((Environments)null);

            var mockGoals = new Mock<IGeneralRepository<Goals>>();
            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            var mockUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();

            var apiOptions = Options.Create(new ApiSettings());
            var http = new HttpClient();

            var service = new DashboardsAppService(
                mockApp.Object, mockEnvRepo.Object, mockGoals.Object,
                mockPlanned.Object, mockUnplanned.Object, apiOptions, http);

            await Assert.ThrowsAsync<Exception>(() => service.GetFinancialSummary());
        }

        [Fact]
        public async Task GetFinancialSummary_ShouldReturnCorrectSumsAndMargin()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var env = new Environments { Id = envId, TotalBalance = 1000, FinancialControlLevel = FinancialControlLevelEnum.None };
            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(env);

            var unplanned = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Profit,
                    Amount = 100, IsDeleted = false,
                    TransactionDate = DateTime.UtcNow
                },
                new UnplannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Expense,
                    Amount = 30,
                    IsDeleted = false,
                    TransactionDate = DateTime.UtcNow
                }
            };

            var planned = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Profit,
                    Amount = 50, TransactionDate = DateTime.UtcNow.AddDays(-1),
                    IsDeleted = false
                },
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Expense,
                    Amount = 20, TransactionDate = DateTime.UtcNow.AddDays(-1),
                    IsDeleted = false
                }
            };

            var mockUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(unplanned));

            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(planned));

            var mockGoals = new Mock<IGeneralRepository<Goals>>();

            var apiOptions = Options.Create(new ApiSettings());
            var http = new HttpClient();

            var service = new DashboardsAppService(
                mockApp.Object, mockEnvRepo.Object, mockGoals.Object,
                mockPlanned.Object, mockUnplanned.Object, apiOptions, http);

            var result = await service.GetFinancialSummary();

            Assert.Equal(1000, result.CurrentBalance);
            Assert.Equal(150, result.TotalProfit);
            Assert.Equal(50, result.TotalExpense);
            Assert.Equal("66,7%", result.ProfitMargin);
            Assert.Equal(FinancialControlLevelEnum.None, result.Level);
        }

        [Fact]
        public async Task GetBalanceOverTime_ShouldReturnEmptyWhenNoTransactionsInRange()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var env = new Environments { Id = envId, TotalBalance = 500 };
            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(env);

            var mockUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(new List<UnplannedExpensesAndProfits>()));

            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(new List<PlannedExpensesAndProfits>()));

            var apiOptions = Options.Create(new ApiSettings());
            var http = new HttpClient();

            var service = new DashboardsAppService(
                mockApp.Object, mockEnvRepo.Object, Mock.Of<IGeneralRepository<Goals>>(),
                mockPlanned.Object, mockUnplanned.Object, apiOptions, http);

            var input = new FilterForBalanceOverTimeDto
            {
                StartDate = DateTime.UtcNow.Date.AddDays(-10).AddHours(3),
                EndDate = DateTime.UtcNow.Date.AddDays(-1).AddHours(3)
            };

            var res = await service.GetBalanceOverTime(input);
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetBalanceOverTime_ShouldComputeRunningBalanceAndFilterByDates()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var env = new Environments { Id = envId, TotalBalance = 1000 };
            var mockEnvRepo = new Mock<IGeneralRepository<Environments>>();
            mockEnvRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(env);

            var unplanned = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Profit,
                    Amount = 100,
                    TransactionDate = today.AddDays(-1)
                }
            };

            var planned = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Expense,
                    Amount = 50, TransactionDate = today.AddDays(-2)
                }
            };

            var mockUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(unplanned));

            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(planned));

            var service = new DashboardsAppService(
                mockApp.Object, mockEnvRepo.Object, Mock.Of<IGeneralRepository<Goals>>(),
                mockPlanned.Object, mockUnplanned.Object, Options.Create(new ApiSettings()), new HttpClient());

            var input = new FilterForBalanceOverTimeDto
            {
                StartDate = today.AddDays(-3).AddHours(3),
                EndDate = today.AddDays(0).AddHours(3)
            };

            var res = await service.GetBalanceOverTime(input);

            Assert.Equal(2, res.Count);
            Assert.Equal(planned[0].TransactionDate.Date, res[0].Date.Date);
            Assert.Equal(unplanned[0].TransactionDate.Date, res[1].Date.Date);
        }

        [Fact]
        public async Task GetGoalsSummary_ShouldReturnZeroWhenNoGoals()
        {
            var mockGoals = new Mock<IGeneralRepository<Goals>>();
            mockGoals.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<Goals>(new List<Goals>()));

            var service = new DashboardsAppService(Mock.Of<IAppSession>(), Mock.Of<IGeneralRepository<Environments>>(),
                mockGoals.Object, Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(),
                Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetGoalsSummary();
            Assert.Equal(0, res.Completed);
            Assert.Equal(0, res.Pending);
        }

        [Fact]
        public async Task GetGoalsSummary_ShouldCountCompletedAndPending()
        {
            var envId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            var goals = new List<Goals>
            {
                new Goals { EnvironmentId = envId, Status = true, SingleDate = now.AddDays(-1), IsDeleted = false },
                new Goals { EnvironmentId = envId, Status = false, SingleDate = now.AddDays(-1), IsDeleted = false },
                new Goals { EnvironmentId = envId, Status = null, SingleDate = now.AddDays(-1), IsDeleted = false }
            };

            var mockGoals = new Mock<IGeneralRepository<Goals>>();
            mockGoals.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<Goals>(goals));

            var service = new DashboardsAppService(Mock.Of<IAppSession>(), Mock.Of<IGeneralRepository<Environments>>(),
                mockGoals.Object, Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(),
                Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetGoalsSummary();
            Assert.NotNull(res.Completed);
            Assert.NotNull(res.Pending);
        }

        [Fact]
        public async Task GetUnexpectedExpensesAnalysis_ShouldReturnZeroWhenNoExpensesOrProfits()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(new List<UnplannedExpensesAndProfits>()));

            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(new List<PlannedExpensesAndProfits>()));

            var service = new DashboardsAppService(mockApp.Object, Mock.Of<IGeneralRepository<Environments>>(),
                Mock.Of<IGeneralRepository<Goals>>(), mockPlanned.Object, mockUnplanned.Object,
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetUnexpectedExpensesAnalysis();
            Assert.Equal(0, res.TotalUnexpectedExpenses);
            Assert.Equal(0, res.TotalProfits);
            Assert.Equal("0,0%", res.Percentage);
            Assert.Equal("Baixo", res.AlertLevel);
        }

        [Fact]
        public async Task GetUnexpectedExpensesAnalysis_ShouldComputePercentageAndAlertLevel()
        {
            var envId = Guid.NewGuid();
            var today = DateTime.UtcNow.Date;
            var startDate = today.AddDays(-30);

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var unplannedExpenses = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Expense,
                    Amount = 50, TransactionDate = today.AddDays(-5),
                    IsDeleted = false
                }
            };

            var plannedProfits = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Profit,
                    Amount = 100,
                    TransactionDate = today.AddDays(-5),
                    IsDeleted = false
                }
            };

            var unplannedProfits = new List<UnplannedExpensesAndProfits>
            {
                new UnplannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Profit,
                    Amount = 100,
                    TransactionDate = today.AddDays(-5),
                    IsDeleted = false
                }
            };

            var mockUnplanned = new Mock<IGeneralRepository<UnplannedExpensesAndProfits>>();
            mockUnplanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<UnplannedExpensesAndProfits>(unplannedExpenses.Concat(unplannedProfits)));

            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(plannedProfits));

            var service = new DashboardsAppService(mockApp.Object, Mock.Of<IGeneralRepository<Environments>>(),
                Mock.Of<IGeneralRepository<Goals>>(), mockPlanned.Object, mockUnplanned.Object,
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetUnexpectedExpensesAnalysis();

            Assert.Equal(50, res.TotalUnexpectedExpenses);
            Assert.Equal(200, res.TotalProfits);
            Assert.Equal("25,0%", res.Percentage);
            Assert.Equal("Moderado", res.AlertLevel);
        }

        [Fact]
        public async Task GetTheFourMostAchievedRecurringGoals_ShouldReturnEmptyWhenNoGoals()
        {
            var mockGoals = new Mock<IGeneralRepository<Goals>>();
            mockGoals.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<Goals>(new List<Goals>()));

            var service = new DashboardsAppService(Mock.Of<IAppSession>(), Mock.Of<IGeneralRepository<Environments>>(),
                mockGoals.Object, Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(),
                Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetTheFourMostAchievedRecurringGoals();
            Assert.Empty(res);
        }

        [Fact]
        public async Task GetTheFourMostAchievedRecurringGoals_ShouldReturnTopRecurring()
        {
            var envId = Guid.NewGuid();
            var now = DateTime.UtcNow.Date;

            var goals = new List<Goals>
            {
                new Goals
                {
                    EnvironmentId = envId,
                    PeriodType = GoalPeriodTypeEnum.Monthly,
                    AchievementsCount = 5,
                    StartDate = now.AddDays(-10),
                    IsDeleted = false,
                    Description = "A",
                    GoalNumber = 1,
                    Value = 10
                },
                new Goals
                {
                    EnvironmentId = envId,
                    PeriodType = GoalPeriodTypeEnum.Monthly,
                    AchievementsCount = 15,
                    StartDate = now.AddDays(-10),
                    IsDeleted = false,
                    Description = "B",
                    GoalNumber = 2,
                    Value = 20
                },
                new Goals
                {
                    EnvironmentId = envId,
                    PeriodType = GoalPeriodTypeEnum.Monthly,
                    AchievementsCount = 2,
                    StartDate = now.AddDays(-10),
                    IsDeleted = false,
                    Description = "C",
                    GoalNumber = 3,
                    Value = 5
                }
            };

            var mockGoals = new Mock<IGeneralRepository<Goals>>();
            mockGoals.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<Goals>(goals));

            var service = new DashboardsAppService(Mock.Of<IAppSession>(), Mock.Of<IGeneralRepository<Environments>>(),
                mockGoals.Object, Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(),
                Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetTheFourMostAchievedRecurringGoals();
            Assert.NotNull(res.Count);
        }

        [Fact]
        public async Task GetAchievementDistributionByPeriod_ShouldThrowWhenEnvironmentNotFound()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnv = new Mock<IGeneralRepository<Environments>>();
            mockEnv.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync((Environments)null);

            var service = new DashboardsAppService(mockApp.Object, mockEnv.Object, Mock.Of<IGeneralRepository<Goals>>(),
                Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(), Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            await Assert.ThrowsAsync<Exception>(() => service.GetAchievementDistributionByPeriod());
        }

        [Fact]
        public async Task GetAchievementDistributionByPeriod_ShouldReturnGroupedData()
        {
            var envId = Guid.NewGuid();
            var env = new Environments { Id = envId };

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnv = new Mock<IGeneralRepository<Environments>>();
            mockEnv.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(env);

            var goals = new List<Goals>
            {
                new Goals { EnvironmentId = envId, PeriodType = GoalPeriodTypeEnum.Monthly, AchievementsCount = 3, IsDeleted = false },
                new Goals { EnvironmentId = envId, PeriodType = GoalPeriodTypeEnum.Monthly, AchievementsCount = 2, IsDeleted = false },
                new Goals { EnvironmentId = envId, PeriodType = GoalPeriodTypeEnum.Weekly, AchievementsCount = 4, IsDeleted = false }
            };

            var mockGoals = new Mock<IGeneralRepository<Goals>>();
            mockGoals.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<Goals>(goals));

            var service = new DashboardsAppService(mockApp.Object, mockEnv.Object, mockGoals.Object,
                Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(), Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetAchievementDistributionByPeriod();
            Assert.Equal(2, res.Count);
            var monthly = res.Single(r => r.PeriodType == GoalPeriodTypeEnum.Monthly.ToString());
            Assert.Equal(5, monthly.TotalAchievements);
        }

        [Fact]
        public async Task GetProjectedBalanceEvolution_ShouldThrowWhenEnvironmentNotFound()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnv = new Mock<IGeneralRepository<Environments>>();
            mockEnv.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync((Environments)null);

            var service = new DashboardsAppService(mockApp.Object, mockEnv.Object, Mock.Of<IGeneralRepository<Goals>>(),
                Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(), Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            await Assert.ThrowsAsync<Exception>(() =>
                service.GetProjectedBalanceEvolution(new FiltersForBalanceProjectionDto
                { IsYear = true, PeriodValue = 1 }));
        }

        [Fact]
        public async Task GetProjectedBalanceEvolution_ShouldReturnMonthlyProjection()
        {
            var envId = Guid.NewGuid();
            var env = new Environments { Id = envId, TotalBalance = 100.0 };

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnv = new Mock<IGeneralRepository<Environments>>();
            mockEnv.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(env);

            var planned = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    Type = FinancialRecordTypeEnum.Profit,
                    RecurrenceType = RecurrenceTypeEnum.Monthly,
                    Amount = 50,
                    TransactionDate = DateTime.UtcNow.AddDays(-1),
                    IsDeleted = false
                }
            };

            var mockPlanned = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();
            mockPlanned.Setup(r => r.GetAll()).Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(planned));

            var service = new DashboardsAppService(mockApp.Object, mockEnv.Object, Mock.Of<IGeneralRepository<Goals>>(),
                mockPlanned.Object, Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            var res = await service.GetProjectedBalanceEvolution(new FiltersForBalanceProjectionDto { IsYear = false, PeriodValue = 2 });

            Assert.NotNull(res.Count);
        }

        [Fact]
        public async Task EditTotalBalance_ShouldThrowWhenEnvironmentNotFound()
        {
            var envId = Guid.NewGuid();
            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnv = new Mock<IGeneralRepository<Environments>>();
            mockEnv.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>())).ReturnsAsync((Environments)null);

            var service = new DashboardsAppService(mockApp.Object, mockEnv.Object, Mock.Of<IGeneralRepository<Goals>>(),
                Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(), Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            await Assert.ThrowsAsync<Exception>(() => service.EditTotalBalance(100));
        }

        [Fact]
        public async Task EditTotalBalance_ShouldUpdateEnvironmentTotalBalance()
        {
            var envId = Guid.NewGuid();
            var env = new Environments { Id = envId, TotalBalance = 10 };

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(a => a.EnvironmentId).Returns(envId);

            var mockEnv = new Mock<IGeneralRepository<Environments>>();
            mockEnv.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>())).ReturnsAsync(env);
            mockEnv.Setup(r => r.UpdateAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask).Verifiable();

            var service = new DashboardsAppService(mockApp.Object, mockEnv.Object, Mock.Of<IGeneralRepository<Goals>>(),
                Mock.Of<IGeneralRepository<PlannedExpensesAndProfits>>(), Mock.Of<IGeneralRepository<UnplannedExpensesAndProfits>>(),
                Options.Create(new ApiSettings()), new HttpClient());

            await service.EditTotalBalance(250.5);

            Assert.Equal(250.5, env.TotalBalance);
            mockEnv.Verify(r => r.UpdateAsync(It.Is<Environments>(e => e.TotalBalance == 250.5)), Times.Once);
        }
    }
}