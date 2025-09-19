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
    }
}