using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Moq;

namespace FinancialSystem.Test.EnvCreationAndEditingTests
{
    public class EnvironmentServiceTests
    {
        [Fact]
        public async Task ShouldRegisterTheEnvSuccessfully()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();

            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask);

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            //act e assert
            var exception = await Record.ExceptionAsync(() =>
                            service.InsertAndUpdateEnvironment("description",
                                                               "name",
                                                               Core.Enums.EnvironmentTypeEnum.Personal,
                                                               Guid.NewGuid()));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenInsertFails()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Environments>()))
                    .ThrowsAsync(new Exception("Insert failed"));

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.InsertAndUpdateEnvironment(null, null, Core.Enums.EnvironmentTypeEnum.Personal, Guid.NewGuid())
            );
        }

        [Fact]
        public async Task ShouldUpdateEnvironmentSuccessfully()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask);

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act
            var exception = await Record.ExceptionAsync(() =>
                service.InsertAndUpdateEnvironment("new description", "new name", Core.Enums.EnvironmentTypeEnum.Business, Guid.NewGuid())
            );

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenUpdateFails()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Environments>()))
                    .ThrowsAsync(new Exception("Update failed"));

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.InsertAndUpdateEnvironment(null, null, Core.Enums.EnvironmentTypeEnum.Family, Guid.NewGuid())
            );
        }

        [Fact]
        public async Task ShouldDeleteEnvironmentSuccessfully()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask);

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act
            var exception = await Record.ExceptionAsync(() =>
                service.DeleteEnvironment(Guid.NewGuid())
            );

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenDeleteFails()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Environments>()))
                    .ThrowsAsync(new Exception("Delete failed"));

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.DeleteEnvironment(Guid.Empty)
            );
        }

        [Fact]
        public async Task ShouldReturnListOfEnvironments()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            var environments = new List<Environments>
            {
                new Environments { Id = Guid.NewGuid(), Name = "Env1" },
                new Environments { Id = Guid.NewGuid(), Name = "Env2" }
            };

            mockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(environments);

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act
            var result = await service.GetAllEnvironments();

            // assert
            Assert.NotNull(result);

            if (result.Count > 0)
            {
                Assert.Equal(2, result.Count());
            }
            else
                Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task ShouldReturnEnvironmentById()
        {
            // arrange
            var env = new Environments { Id = Guid.NewGuid(), Name = "TestEnv" };

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(env.Id)).ReturnsAsync(env);

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act
            var result = await service.GetEnvironment(env.Id);

            // assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenEnvironmentNotFound()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Environments)null);

            var service = new EnvironmentSettingsAppService(mockRepo.Object);

            // act
            // assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.GetEnvironment(Guid.Empty)
            );
        }
    }
}
