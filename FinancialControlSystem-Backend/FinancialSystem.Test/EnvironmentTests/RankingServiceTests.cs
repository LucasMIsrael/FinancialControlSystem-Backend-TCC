using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Moq;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class RankingServiceTests
    {
        [Fact]
        public async Task ShouldThrowException_WhenCurrentEnvironmentNotFound()
        {
            var envId = Guid.NewGuid();

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync((Environments)null);

            var service = new RankingAppService(mockApp.Object, mockRepo.Object);

            await Assert.ThrowsAsync<Exception>(() => service.GetEnvironmentsForRanking());
        }

        [Fact]
        public async Task ShouldReturnOnlySameTypeAndNotDeleted()
        {
            var envId = Guid.NewGuid();

            var currentEnv = new Environments
            {
                Id = envId,
                Type = EnvironmentTypeEnum.Personal
            };

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var list = new List<Environments>
            {
                new Environments { Id = Guid.NewGuid(), Type = EnvironmentTypeEnum.Personal, IsDeleted = false },
                new Environments { Id = Guid.NewGuid(), Type = EnvironmentTypeEnum.Personal, IsDeleted = true },
                new Environments { Id = Guid.NewGuid(), Type = EnvironmentTypeEnum.Business, IsDeleted = false }
            };

            var asyncList = new AsyncEnumerable<Environments>(list);

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(currentEnv);
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new RankingAppService(mockApp.Object, mockRepo.Object);

            var result = await service.GetEnvironmentsForRanking();

            Assert.Single(result);
        }

        [Fact]
        public async Task ShouldMapEnvironmentToRankingDtoCorrectly()
        {
            var envId = Guid.NewGuid();

            var currentEnv = new Environments
            {
                Id = envId,
                Type = EnvironmentTypeEnum.Personal,
            };

            var envUser = new Users { Name = "Carlos" };

            var env = new Environments
            {
                Id = Guid.NewGuid(),
                Type = EnvironmentTypeEnum.Personal,
                User = envUser,
                TotalGoalsAchieved = 5,
                FinancialControlLevel = FinancialControlLevelEnum.None,
            };

            var asyncList = new AsyncEnumerable<Environments>(new List<Environments> { env });

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(currentEnv);
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new RankingAppService(mockApp.Object, mockRepo.Object);

            var result = await service.GetEnvironmentsForRanking();

            Assert.Single(result);
            var dto = result.First();

            Assert.Equal("Carlos", dto.UserName);
            Assert.Equal(5, dto.TotalGoalsAchieved);
            Assert.Equal("", dto.EnvironmentLevel);
            Assert.NotNull(dto.CreationTime);
        }

        [Fact]
        public async Task ShouldSortByTotalGoalsAchievedDescending()
        {
            var envId = Guid.NewGuid();

            var currentEnv = new Environments { Id = envId, Type = EnvironmentTypeEnum.Family };

            var list = new List<Environments>
            {
                new Environments { Type = EnvironmentTypeEnum.Family, TotalGoalsAchieved = 5 },
                new Environments { Type = EnvironmentTypeEnum.Family, TotalGoalsAchieved = 20 },
                new Environments { Type = EnvironmentTypeEnum.Family, TotalGoalsAchieved = 10 }
            };

            var asyncList = new AsyncEnumerable<Environments>(list);

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(currentEnv);
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new RankingAppService(mockApp.Object, mockRepo.Object);

            var result = await service.GetEnvironmentsForRanking();

            Assert.Equal(20, result.First().TotalGoalsAchieved);
        }

        [Fact]
        public async Task ShouldLimitTo10Items()
        {
            var envId = Guid.NewGuid();

            var currentEnv = new Environments { Id = envId, Type = EnvironmentTypeEnum.Business };

            var list = new List<Environments>();

            for (int i = 1; i <= 20; i++)
                list.Add(new Environments
                {
                    Type = EnvironmentTypeEnum.Business,
                    TotalGoalsAchieved = i
                });

            var asyncList = new AsyncEnumerable<Environments>(list);

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.EnvironmentId).Returns(envId);

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(envId)).ReturnsAsync(currentEnv);
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var service = new RankingAppService(mockApp.Object, mockRepo.Object);

            var result = await service.GetEnvironmentsForRanking();

            Assert.Equal(10, result.Count);
        }
    }
}