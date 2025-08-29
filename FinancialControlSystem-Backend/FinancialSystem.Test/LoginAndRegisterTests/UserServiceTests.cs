using FinancialSystem.Application.Services.UserServices;
using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Options;
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
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();

            var user = new UserDataDto
            {
                Name = "João Silva",
                Email = "joao@email.com",
                Password = "123456"
            };

            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockRepo.Object, mockJwtSettings.Object);

            //act e assert
            var exception = await Record.ExceptionAsync(() => service.RegisterUser(user));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldLoginSuccessfullyWithValidCredentials()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();

            var validEmail = "joao@email.com";
            var validPassword = "123456";

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(validPassword);

            var expectedUser = new Users
            {
                Id = 123,
                Name = "João Silva",
                Email = validEmail,
                Password = hashedPassword
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<Expression<Func<Users, bool>>>()
            )).ReturnsAsync((Expression<Func<Users, bool>> predicate) =>
            {
                var compiled = predicate.Compile();
                return compiled(expectedUser) ? expectedUser : null;
            });

            var loginDto = new UserDataDto
            {
                Email = validEmail,
                Password = validPassword
            };

            var jwtSettings = new JwtSettings
            {
                Secret = "u03g*Lp7K#qT^Zb!r9Jx$dE@N8fV!mR2aWcX6sH1"
            };

            var options = Options.Create(jwtSettings);
            var service = new UserSettingsAppService(mockRepo.Object, options);

            //act
            var result = await service.UserLogin(loginDto);

            //assert
            Assert.NotNull(result);
            Assert.Contains(".", result); //verifica retorno tem estrutura de JWT
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenCredentialsAreInvalid()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();

            var validUser = new Users
            {
                Id = 123,
                Name = "João Silva",
                Email = "joao@email.com",
                Password = "123456"
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                .ReturnsAsync((Expression<Func<Users, bool>> predicate) =>
                {
                    var compiled = predicate.Compile();
                    return compiled(validUser) ? validUser : null;
                });

            var jwtSettings = new JwtSettings
            {
                Secret = "u03g*Lp7K#qT^Zb!r9Jx$dE@N8fV!mR2aWcX6sH1"
            };

            var jwtOptions = Options.Create(jwtSettings);
            var service = new UserSettingsAppService(mockRepo.Object, jwtOptions);

            var invalidLogin = new UserDataDto
            {
                Email = "email@errado.com",
                Password = "senhaerrada"
            };

            //act
            var exception = await Assert.ThrowsAsync<Exception>(() => service.UserLogin(invalidLogin));

            //assert
            Assert.Equal("Email ou senha inválidos.", exception.Message);
        }
    }
}