using FinancialSystem.Application.Services.UserSettings;
using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Moq;
using System.Linq.Expressions;

namespace FinancialSystem.Test.LoginAndRegisterTests
{
    public class UserServiceTests
    {
        [Fact]
        public async Task ShouldRegisterUserSuccessfully()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<User>>();
            var user = new UserDataDto
            {
                Name = "João Silva",
                Email = "joao@email.com",
                Password = "123456"
            };

            mockRepo.Setup(r => r.InsertAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockRepo.Object);

            //act e assert
            var exception = await Record.ExceptionAsync(() => service.RegisterAsync(user));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldLoginSuccessfullyWithValidCredentials()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<User>>();

            var validEmail = "joao@email.com";
            var validPassword = "123456";

            var expectedUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "João Silva",
                Email = validEmail,
                Password = validPassword
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<User, bool>>>()
            )).ReturnsAsync((Expression<Func<User, bool>> predicate) =>
            {
                //simula comportamento
                var compiled = predicate.Compile();
                return compiled(expectedUser) ? expectedUser : null;
            });

            var service = new UserSettingsAppService(mockRepo.Object);

            //act
            var result = await service.LoginAsync(validEmail, validPassword);

            //assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.Email, result.Email);
        }

        [Fact]
        public async Task ShouldReturnNullWhenCredentialsAreInvalid()
        {
            // Arrange
            var mockRepo = new Mock<IGeneralRepository<User>>();

            var validUser = new User
            {
                Id = Guid.NewGuid(),
                Name = "João Silva",
                Email = "joao@email.com",
                Password = "123456"
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync((Expression<Func<User, bool>> predicate) =>
                {
                    var compiled = predicate.Compile();
                    return compiled(validUser) ? validUser : null;
                });

            var service = new UserSettingsAppService(mockRepo.Object);

            // Act
            var result = await service.LoginAsync("email@errado.com", "senhaerrada");

            // Assert
            Assert.Null(result);
        }
    }
}
