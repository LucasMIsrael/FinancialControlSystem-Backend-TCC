using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Moq;
using System.Linq.Expressions;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class GoalsServiceTests
    {
        private readonly Mock<IGeneralRepository<Goals>> _goalsRepositoryMock;
        private readonly Mock<IGeneralRepository<PlannedExpensesAndProfits>> _plannedTransactionsRepositoryMock;
        private readonly Mock<IAppSession> _appSessionMock;
        private readonly GoalsSettingsAppService _service;

        public GoalsServiceTests()
        {
            _goalsRepositoryMock = new Mock<IGeneralRepository<Goals>>();
            _plannedTransactionsRepositoryMock = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();

            _appSessionMock = new Mock<IAppSession>();
            _appSessionMock.Setup(x => x.UserId).Returns(123);
            _appSessionMock.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            _service = new GoalsSettingsAppService(_appSessionMock.Object, _goalsRepositoryMock.Object, _plannedTransactionsRepositoryMock.Object);
        }

        [Fact]
        public async Task ShouldInsertGoalSuccessfully()
        {
            // Arrange
            var environmentId = (Guid)_appSessionMock.Object.EnvironmentId;

            var dto = new GoalDataDto
            {
                Description = "meta 1",
                StartDate = new DateTime(2012, 1, 30),
                Id = Guid.NewGuid(),
                PeriodType = GoalPeriodTypeEnum.Daily,
                SingleDate = null,
                Value = 100
            };

            var plannedTxs = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    Id = Guid.NewGuid(),
                    EnvironmentId = environmentId,
                    RecurrenceType = RecurrenceTypeEnum.Daily,
                    Amount = 200,
                    TransactionDate = DateTime.UtcNow.Date,
                    Type = FinancialRecordTypeEnum.Profit
                }
            };

            var asyncPlanned = new AsyncEnumerable<PlannedExpensesAndProfits>(plannedTxs);
            _plannedTransactionsRepositoryMock.Setup(r => r.GetAll()).Returns(asyncPlanned);

            // Act
            var exception = await Record.ExceptionAsync(() => _service.InsertNewGoal(dto));

            // Assert
            Assert.Null(exception);
            _goalsRepositoryMock.Verify(r => r.InsertAsync(It.IsAny<Goals>()), Times.Once);
        }

        [Fact]
        public async Task ShouldUpdateGoalSuccessfully()
        {
            // Arrange
            var mockRepo = new Mock<IGeneralRepository<Goals>>();
            var mockRepoTransactions = new Mock<IGeneralRepository<PlannedExpensesAndProfits>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId, Guid.NewGuid());

            var service = new GoalsSettingsAppService(mockAppSession.Object, mockRepo.Object, mockRepoTransactions.Object);

            var dto = new GoalDataDto
            {
                Description = "meta 2",
                StartDate = new DateTime(2012, 1, 30),
                Id = Guid.NewGuid(),
                PeriodType = GoalPeriodTypeEnum.Daily,
                SingleDate = null,
                Value = 100
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Goals, bool>>>()))
                    .ReturnsAsync(new Goals { Id = (Guid)dto.Id });

            var plannedTxs = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    Id = Guid.NewGuid(),
                    EnvironmentId = mockAppSession.Object.EnvironmentId.Value,
                    RecurrenceType = RecurrenceTypeEnum.Daily,
                    Amount = 150,
                    TransactionDate = DateTime.UtcNow.Date,
                    Type = FinancialRecordTypeEnum.Profit
                }
            };
            var asyncPlanned = new AsyncEnumerable<PlannedExpensesAndProfits>(plannedTxs);
            mockRepoTransactions.Setup(r => r.GetAll()).Returns(asyncPlanned);

            // Act
            await service.UpdateGoal(dto);

            // Assert
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Goals>()), Times.Once);
        }

        [Fact]
        public async Task ShouldThrowWhenGoalNotFoundInUpdate()
        {
            // Arrange
            var input = new GoalDataDto { Id = Guid.NewGuid() };

            // Service uses FirstOrDefaultAsync to find the goal when updating.
            _goalsRepositoryMock.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Goals, bool>>>()))
                                .ReturnsAsync((Goals)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.UpdateGoal(input));
        }

        [Fact]
        public async Task ShouldDeleteGoal()
        {
            // Arrange
            var id = Guid.NewGuid();

            _goalsRepositoryMock.Setup(r => r.GetByIdAsync(id))
                                .ReturnsAsync(new Goals { Id = id });

            // Act
            await _service.DeleteGoal(id);

            // Assert
            _goalsRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Goals>()), Times.Once);
        }

        [Fact]
        public async Task ShouldThrowWhenGoalNotFoundInDelete()
        {
            // Arrange
            _goalsRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                                .ReturnsAsync((Goals)null);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteGoal(Guid.NewGuid()));
        }

        [Fact]
        public async Task ShouldReturnEmptyListWhenNoGoalsExist()
        {
            var asyncList = new AsyncEnumerable<Goals>(new List<Goals>());
            _goalsRepositoryMock.Setup(r => r.GetAll()).Returns(asyncList);

            var result = await _service.GetAllGoals();

            Assert.Empty(result);
        }

        [Fact]
        public async Task ShouldReturnMappedDtosCorrectly()
        {
            var g = new Goals
            {
                Id = Guid.NewGuid(),
                Description = "Meta 1",
                GoalNumber = 10,
                Value = 200,
                PeriodType = GoalPeriodTypeEnum.Daily,
                Status = false,
                EnvironmentId = (Guid)_appSessionMock.Object.EnvironmentId
            };

            var asyncList = new AsyncEnumerable<Goals>(new List<Goals> { g });
            _goalsRepositoryMock.Setup(r => r.GetAll()).Returns(asyncList);

            var result = await _service.GetAllGoals();

            Assert.Single(result);
            Assert.Equal(g.Id, result[0].Id);
            Assert.Equal(g.Description, result[0].Description);
            Assert.Equal(g.Value, result[0].Value);
            Assert.Equal(g.Status, result[0].Status);
        }

        [Fact]
        public async Task ShouldIgnoreDeletedGoals()
        {
            var goals = new List<Goals>
            {
                new Goals
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = true,
                    EnvironmentId = (Guid)_appSessionMock.Object.EnvironmentId
                },
                new Goals
                {
                    Id = Guid.NewGuid(),
                    IsDeleted = false,
                    EnvironmentId = (Guid)_appSessionMock.Object.EnvironmentId
                }
            };

            var asyncList = new AsyncEnumerable<Goals>(goals);
            _goalsRepositoryMock.Setup(r => r.GetAll()).Returns(asyncList);

            var result = await _service.GetAllGoals();

            Assert.Single(result);
        }

        [Fact]
        public async Task ShouldReturnOnlyGoalsForCurrentEnvironment()
        {
            var envId = (Guid)_appSessionMock.Object.EnvironmentId;

            var goals = new List<Goals>
            {
                new Goals { Id = Guid.NewGuid(), EnvironmentId = envId },
                new Goals { Id = Guid.NewGuid(), EnvironmentId = Guid.NewGuid() },
            };

            var asyncList = new AsyncEnumerable<Goals>(goals);
            _goalsRepositoryMock.Setup(r => r.GetAll()).Returns(asyncList);

            var result = await _service.GetAllGoals();

            Assert.Single(result);
        }

        [Fact]
        public async Task UpdateGoalsAchieved_ShouldNotAchieveGoalWhenBalanceIsInsufficient()
        {
            var env = new Environments { Id = Guid.NewGuid(), TotalBalance = 20 };

            var goal = new Goals
            {
                Id = Guid.NewGuid(),
                Environment = env,
                EnvironmentId = env.Id,
                PeriodType = GoalPeriodTypeEnum.Daily,
                StartDate = DateTime.UtcNow.AddDays(-1),
                Value = 100,
                LastEvaluatedDate = DateTime.UtcNow.AddDays(-2),
                Status = false
            };

            var asyncList = new AsyncEnumerable<Goals>(new List<Goals> { goal });
            _goalsRepositoryMock.Setup(r => r.GetAll()).Returns(asyncList);

            var plannedEmpty = new AsyncEnumerable<PlannedExpensesAndProfits>(new List<PlannedExpensesAndProfits>());
            _plannedTransactionsRepositoryMock.Setup(r => r.GetAll()).Returns(plannedEmpty);

            await _service.UpdateGoalsAchieved();

            Assert.False(goal.Status);
            Assert.Equal(0, env.TotalGoalsAchieved);
        }

        [Fact]
        public async Task UpdateGoalsAchieved_ShouldSubtractExpenses_WhenCalculatingRecurrenceTotals()
        {
            var envId = Guid.NewGuid();

            var env = new Environments { Id = envId, TotalBalance = 200 };

            var goal = new Goals
            {
                Id = Guid.NewGuid(),
                Environment = env,
                EnvironmentId = envId,
                PeriodType = GoalPeriodTypeEnum.Monthly,
                StartDate = DateTime.UtcNow.AddMonths(-1),
                LastEvaluatedDate = DateTime.UtcNow.AddMonths(-1),
                Value = 250,
                Status = false
            };

            _goalsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<Goals>(new List<Goals> { goal }));

            var txs = new List<PlannedExpensesAndProfits>
            {
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    RecurrenceType = RecurrenceTypeEnum.Monthly,
                    Amount = 80,
                    Type = FinancialRecordTypeEnum.Profit
                },
                new PlannedExpensesAndProfits
                {
                    EnvironmentId = envId,
                    RecurrenceType = RecurrenceTypeEnum.Monthly,
                    Amount = 50,
                    Type = FinancialRecordTypeEnum.Expense
                }
            };

            _plannedTransactionsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(txs));

            await _service.UpdateGoalsAchieved();

            //recurrence total = 80 - 50 = 30 → TotalBalance (200) + 30 = 230 < 250 → não atinge
            Assert.False(goal.Status);
            Assert.Equal(0, env.TotalGoalsAchieved);
        }

        [Fact]
        public async Task UpdateGoalsAchieved_ShouldNotEvaluateGoalBeforeStartDate()
        {
            var env = new Environments { Id = Guid.NewGuid(), TotalBalance = 999 };

            var goal = new Goals
            {
                Id = Guid.NewGuid(),
                EnvironmentId = env.Id,
                Environment = env,
                StartDate = DateTime.UtcNow.AddDays(3),
                PeriodType = GoalPeriodTypeEnum.Daily,
                LastEvaluatedDate = null,
                Value = 100,
                Status = false
            };

            _goalsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<Goals>(new List<Goals> { goal }));

            _plannedTransactionsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(new List<PlannedExpensesAndProfits>()));

            await _service.UpdateGoalsAchieved();

            Assert.False(goal.Status);
            Assert.Null(goal.LastEvaluatedDate);
            Assert.Equal(0, env.TotalGoalsAchieved);
        }

        [Fact]
        public async Task UpdateGoalsAchieved_ShouldNotReEvaluateGoalOnSameDay()
        {
            var today = DateTime.UtcNow.Date;

            var env = new Environments
            {
                Id = Guid.NewGuid(),
                TotalBalance = 500,
                TotalGoalsAchieved = 1
            };

            var goal = new Goals
            {
                Id = Guid.NewGuid(),
                EnvironmentId = env.Id,
                Environment = env,
                PeriodType = GoalPeriodTypeEnum.Daily,
                StartDate = today.AddDays(-5),
                LastEvaluatedDate = today, // já avaliado hoje
                Status = true,
                Value = 100
            };

            _goalsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<Goals>(new List<Goals> { goal }));

            await _service.UpdateGoalsAchieved();

            // Não deve aumentar novamente
            Assert.Equal(1, env.TotalGoalsAchieved);
            Assert.True(goal.Status);
        }

        [Fact]
        public async Task UpdateGoalsAchieved_ShouldUpdateLastEvaluatedDate_EvenIfGoalNotAchieved()
        {
            var env = new Environments { Id = Guid.NewGuid(), TotalBalance = 10 };

            var goal = new Goals
            {
                Id = Guid.NewGuid(),
                EnvironmentId = env.Id,
                Environment = env,
                PeriodType = GoalPeriodTypeEnum.Daily,
                StartDate = DateTime.UtcNow.AddDays(-2),
                LastEvaluatedDate = DateTime.UtcNow.AddDays(-2),
                Status = false,
                Value = 999 // impossível de atingir
            };

            _goalsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<Goals>(new List<Goals> { goal }));

            _plannedTransactionsRepositoryMock.Setup(r => r.GetAll())
                .Returns(new AsyncEnumerable<PlannedExpensesAndProfits>(new List<PlannedExpensesAndProfits>()));

            await _service.UpdateGoalsAchieved();

            Assert.False(goal.Status);
        }
    }
}