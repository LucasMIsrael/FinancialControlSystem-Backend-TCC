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
        private readonly Mock<IAppSession> _appSessionMock;
        private readonly GoalsSettingsAppService _service;

        public GoalsServiceTests()
        {
            _goalsRepositoryMock = new Mock<IGeneralRepository<Goals>>();

            _appSessionMock = new Mock<IAppSession>();
            _appSessionMock.Setup(x => x.UserId).Returns(123);
            _appSessionMock.Setup(x => x.EnvironmentId).Returns(Guid.NewGuid());

            _service = new GoalsSettingsAppService(_appSessionMock.Object, _goalsRepositoryMock.Object);
        }

        [Fact]
        public async Task ShouldInsertGoalSuccessfully()
        {
            // Arrange
            var dto = new GoalDataDto
            {
                Description = "meta 1",
                StartDate = new DateTime(30 / 01 / 2012),
                Id = Guid.NewGuid(),
                PeriodType = GoalPeriodTypeEnum.Daily,
                SingleDate = null,
                Value = 100
            };

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

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new GoalsSettingsAppService(mockAppSession.Object, mockRepo.Object);

            var dto = new GoalDataDto
            {
                Description = "meta 2",
                StartDate = new DateTime(30 / 01 / 2012),
                Id = Guid.NewGuid(),
                PeriodType = GoalPeriodTypeEnum.Daily,
                SingleDate = null,
                Value = 100
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Goals, bool>>>()))
                    .ReturnsAsync(new Goals { Id = (Guid)dto.Id });

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

            _goalsRepositoryMock.Setup(x => x.GetByIdAsync((Guid)input.Id))
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

            await _service.UpdateGoalsAchieved();

            Assert.False(goal.Status);
            Assert.Equal(0, env.TotalGoalsAchieved);
        }
    }
}