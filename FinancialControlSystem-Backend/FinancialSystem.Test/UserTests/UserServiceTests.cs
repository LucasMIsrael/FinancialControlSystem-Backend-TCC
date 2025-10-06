using FinancialSystem.Application.Services.UserServices;
using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Options;
using Moq;
using System.Linq.Expressions;

namespace FinancialSystem.Test.UserTests
{
    public class UserServiceTests
    {
        [Fact]
        public async Task ShouldRegisterUserSuccessfully()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var user = new UserDataDto
            {
                Name = "João Silva",
                Email = "joao@email.com",
                Password = "123456"
            };

            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object);

            //act e assert
            var exception = await Record.ExceptionAsync(() => service.RegisterUser(user));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldLoginSuccessfullyWithValidCredentials()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

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
            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, options);

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
            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

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
            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, jwtOptions);

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

        [Fact]
        public async Task ShouldReturnUserInformationsSuccessfully()
        {
            // arrange
            var user = new Users { Id = 123, Name = "John Doe", Email = "john@test.com" };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(user);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object);

            // act
            var result = await service.GetUserInformations();

            // assert
            Assert.NotNull(result);
            Assert.Equal(user.Name, result.Name);
            Assert.Equal(user.Email, result.Email);
            Assert.Equal(user.Id, result.Id);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenUserNotFoundOnGetInformations()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync((Users)null);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.GetUserInformations());
        }

        [Fact]
        public async Task ShouldUpdateUserInformationsSuccessfully()
        {
            // arrange
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword("oldpass");
            var existingUser = new Users { Id = 123, Name = "Old Name", Email = "old@test.com", Password = hashedPassword };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(existingUser);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object);

            var input = new UserDataForUpdateDto
            {
                Id = 123,
                Name = "New Name",
                Email = "new@test.com",
                OldPassword = "oldpass",
                NewPassword = "newpass"
            };

            // act
            var exception = await Record.ExceptionAsync(() => service.UpdateUserInformations(input));

            // assert
            Assert.Null(exception);
            mockRepo.Verify(r => r.UpdateAsync(It.Is<Users>(u =>
                u.Name == "New Name" &&
                u.Email == "new@test.com"
            )), Times.Once);
        }

        [Fact]
        public async Task ShouldThrowExceptionWhenUserNotFoundOnUpdate()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync((Users)null);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object);

            var input = new UserDataForUpdateDto
            {
                Id = 123,
                Name = "Any",
                Email = "any@test.com",
                OldPassword = "oldpass",
                NewPassword = "newpass"
            };

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.UpdateUserInformations(input));
        }
    }
}