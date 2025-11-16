using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace FinancialSystem.Test.EnvironmentTests
{
    public class EnvironmentServiceTests
    {
        [Fact]
        public async Task ShouldInsertEnvironmentSuccessfully()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123); // simula usuário logado
            mockAppSession.SetupProperty(x => x.EnvironmentId); // se precisar setar no fluxo

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            var input = new EnvironmentDataDto
            {
                Description = "desc",
                Name = "EnvTest",
                Type = Core.Enums.EnvironmentTypeEnum.Personal
            };

            // act
            var exception = await Record.ExceptionAsync(() => service.InsertEnvironment(input));

            // assert
            Assert.Null(exception);
            mockRepo.Verify(r => r.InsertAsync(It.IsAny<Environments>()), Times.Once);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenInsertFails()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Environments>()))
                    .ThrowsAsync(new Exception("Insert failed"));

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            var input = new EnvironmentDataDto { Description = "desc", Name = "EnvTest" };

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.InsertEnvironment(input));
        }

        [Fact]
        public async Task ShouldUpdateEnvironmentSuccessfully()
        {
            // arrange
            var existingEnv = new Environments { Id = Guid.NewGuid(), Name = "Old", Description = "Old Desc" };

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                    .ReturnsAsync(existingEnv);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            var input = new EnvironmentDataDto
            {
                Id = existingEnv.Id,
                Description = "New Desc",
                Name = "New Name",
                Type = Core.Enums.EnvironmentTypeEnum.Business
            };

            // act
            var exception = await Record.ExceptionAsync(() => service.UpdateEnvironment(input));

            // assert
            Assert.Null(exception);
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Environments>()), Times.Once);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenEnvironmentNotFoundOnUpdate()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                    .ReturnsAsync((Environments)null);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            var input = new EnvironmentDataDto
            {
                Id = Guid.NewGuid(),
                Description = "New Desc",
                Name = "New Name",
                Type = Core.Enums.EnvironmentTypeEnum.Business
            };

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.UpdateEnvironment(input));
        }

        [Fact]
        public async Task ShouldDeleteEnvironmentSuccessfully()
        {
            // arrange
            var env = new Environments { Id = Guid.NewGuid(), Name = "EnvTest" };

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                    .ReturnsAsync(env);
            mockRepo.Setup(r => r.DeleteAsync(It.IsAny<Environments>())).Returns(Task.CompletedTask);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            // act
            var exception = await Record.ExceptionAsync(() => service.DeleteEnvironment(env.Id));

            // assert
            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenDeleteEnvironmentNotFound()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                    .ReturnsAsync((Environments)null);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.DeleteEnvironment(Guid.NewGuid()));
        }

        [Fact]
        public async Task ShouldReturnEnvironmentById()
        {
            // arrange
            var env = new Environments { Id = Guid.NewGuid(), Name = "TestEnv", UserID = 123 };

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                    .ReturnsAsync(env);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            // act
            var result = await service.GetEnvironment(env.Id);

            // assert
            Assert.NotNull(result);
            Assert.Equal(env.Name, result.Name);
        }

        [Fact]
        public async Task ShouldReturnEmptyDtoWhenEnvironmentNotFound()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Environments, bool>>>()))
                    .ReturnsAsync((Environments)null);

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(mockAppSession.Object,
                                                            mockRepo.Object,
                                                            mockLogger.Object);

            // act
            var result = await service.GetEnvironment(Guid.NewGuid());

            // assert
            Assert.NotNull(result);
            Assert.Equal(null, result.Id);
        }

        [Fact]
        public async Task ShouldReturnEmptyList_WhenNoEnvironmentsExist()
        {
            var userId = 123L;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.UserId).Returns(userId);

            var asyncList = new AsyncEnumerable<Environments>(new List<Environments>());

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(
                mockApp.Object, mockRepo.Object, mockLogger.Object);

            var result = await service.GetAllEnvironments();

            Assert.Empty(result);
        }

        [Fact]
        public async Task ShouldReturnOnlyUserEnvironments()
        {
            var userId = 99L;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.UserId).Returns(userId);

            var list = new List<Environments>
            {
                new Environments { Id = Guid.NewGuid(), UserID = userId, Name = "Env1" },
                new Environments { Id = Guid.NewGuid(), UserID = 555, Name = "Env2" },
                new Environments { Id = Guid.NewGuid(), UserID = userId, Name = "Env3" }
            };

            var asyncList = new AsyncEnumerable<Environments>(list);

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(
                mockApp.Object, mockRepo.Object, mockLogger.Object);

            var result = await service.GetAllEnvironments();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Name == "Env1");
            Assert.Contains(result, r => r.Name == "Env3");
        }

        [Fact]
        public async Task ShouldMapEnvironmentPropertiesCorrectly()
        {
            var userId = 440L;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.UserId).Returns(userId);

            var env = new Environments
            {
                Id = Guid.NewGuid(),
                Name = "Casa",
                Description = "Ambiente pessoal",
                UserID = userId,
                Type = EnvironmentTypeEnum.Personal
            };

            var asyncList = new AsyncEnumerable<Environments>(
                new List<Environments> { env }
            );

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(
                mockApp.Object, mockRepo.Object, mockLogger.Object);

            var result = await service.GetAllEnvironments();

            Assert.Single(result);
            Assert.Equal(env.Id, result[0].Id);
            Assert.Equal(env.Name, result[0].Name);
            Assert.Equal(env.Description, result[0].Description);
            Assert.Equal(env.Type, result[0].Type);
        }

        [Fact]
        public async Task ShouldReturnEnvironmentsOrderedByCreationTime()
        {
            var userId = 77L;

            var mockApp = new Mock<IAppSession>();
            mockApp.Setup(x => x.UserId).Returns(userId);

            var env1 = new Environments { Id = Guid.NewGuid(), UserID = userId, Name = "A", CreationTime = DateTime.UtcNow.AddHours(-5) };
            var env2 = new Environments { Id = Guid.NewGuid(), UserID = userId, Name = "B", CreationTime = DateTime.UtcNow.AddHours(-2) };
            var env3 = new Environments { Id = Guid.NewGuid(), UserID = userId, Name = "C", CreationTime = DateTime.UtcNow.AddHours(-1) };

            var asyncList = new AsyncEnumerable<Environments>
            (
                new List<Environments> { env3, env1, env2 }
            );

            var mockRepo = new Mock<IGeneralRepository<Environments>>();
            mockRepo.Setup(r => r.GetAll()).Returns(asyncList);

            var mockLogger = new Mock<ILogger<EnvironmentSettingsAppService>>();

            var service = new EnvironmentSettingsAppService(
                mockApp.Object, mockRepo.Object, mockLogger.Object);

            var result = await service.GetAllEnvironments();

            Assert.Equal(3, result.Count);
            Assert.Equal("A", result[0].Name);
            Assert.Equal("B", result[1].Name);
            Assert.Equal("C", result[2].Name);
        }
    }
}