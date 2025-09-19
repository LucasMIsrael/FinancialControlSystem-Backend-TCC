using FinancialSystem.Application.Services.EnvironmentServices;
using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.DeleteEnvironment(Guid.NewGuid()));
        }

        [Fact]
        public async Task ShouldReturnListOfEnvironments()
        {
            // arrange
            //var environments = new List<Environments>
            //{
            //    new Environments { Id = Guid.NewGuid(), Name = "Env1", UserID = 123 },
            //    new Environments { Id = Guid.NewGuid(), Name = "Env2", UserID = 123 }
            //};

            //var mockRepo = new Mock<IGeneralRepository<Environments>>();
            //mockRepo.Setup(r => r.GetAll()).Returns(environments.AsQueryable());

            //var mockHttp = new Mock<IHttpContextAccessor>();
            //var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "123") };
            //var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            //var context = new DefaultHttpContext { User = user };
            //mockHttp.Setup(x => x.HttpContext).Returns(context);

            //var service = new EnvironmentSettingsAppService(mockRepo.Object, mockHttp.Object);

            //// act
            //var result = await service.GetAllEnvironments();

            //// assert
            //Assert.Equal(2, result.Count);
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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

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

            var service = new EnvironmentSettingsAppService(mockAppSession.Object, mockRepo.Object);

            // act
            var result = await service.GetEnvironment(Guid.NewGuid());

            // assert
            Assert.NotNull(result);
            Assert.Equal(null, result.Id);
        }
    }
}
