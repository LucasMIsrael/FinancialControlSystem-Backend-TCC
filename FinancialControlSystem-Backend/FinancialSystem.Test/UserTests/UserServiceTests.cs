using FinancialSystem.Application.Services.UserServices;
using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;

namespace FinancialSystem.Test.UserTests
{
    public class UserServiceTests
    {
        #region RegisterTests
        [Fact]
        public async Task ShouldRegisterUserSuccessfully()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

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

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object,
                                                     mockJwtSettings.Object,
                                                     mockLogger.Object);

            //act e assert
            var exception = await Record.ExceptionAsync(() => service.RegisterUser(user));

            Assert.Null(exception);
        }

        [Fact]
        public async Task ShouldExceptionWhenInputIsNull()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            await Assert.ThrowsAsync<Exception>(() => service.RegisterUser(null));
        }

        [Fact]
        public async Task ShouldExceptionWhenUserAlreadyExists()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(new Users { Email = "joao@email.com" });

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            var dto = new UserDataDto
            {
                Name = "João",
                Email = "joao@email.com",
                Password = "123"
            };

            var ex = await Assert.ThrowsAsync<Exception>(() => service.RegisterUser(dto));
            Assert.Equal("Já existe um usuário cadastrado com este email", ex.Message);
        }

        [Theory]
        [InlineData("", "Nome", "123")]
        [InlineData("email@email.com", "", "123")]
        [InlineData("email@email.com", "Nome", "")]
        public async Task ShouldExceptionWhenRequiredFieldIsInvalid(string email, string name, string password)
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>())).ReturnsAsync((Users)null);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            var dto = new UserDataDto
            {
                Email = email,
                Name = name,
                Password = password
            };

            var ex = await Assert.ThrowsAsync<Exception>(() => service.RegisterUser(dto));
            Assert.Equal("Campo obrigatório com valor inválido!", ex.Message);
        }

        [Fact]
        public async Task ShouldSanitizeFields()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>())).ReturnsAsync((Users)null);

            Users savedUser = null;
            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Users>()))
                    .Callback<Users>(u => savedUser = u)
                    .Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            var dto = new UserDataDto
            {
                Name = "   João   ",
                Email = "  joao@email.com ",
                Password = " 123456 "
            };

            await service.RegisterUser(dto);

            Assert.Equal("João", savedUser.Name);
            Assert.Equal("joao@email.com", savedUser.Email);
        }

        [Fact]
        public async Task ShouldHashPassword()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>())).ReturnsAsync((Users)null);

            Users savedUser = null;
            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Users>()))
                    .Callback<Users>(u => savedUser = u)
                    .Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            var dto = new UserDataDto
            {
                Name = "João",
                Email = "joao@email.com",
                Password = "123456"
            };

            await service.RegisterUser(dto);

            Assert.NotEqual("123456", savedUser.Password);
            Assert.True(BCrypt.Net.BCrypt.Verify("123456", savedUser.Password));
        }

        [Fact]
        public async Task ShouldCallInsertAsyncOnce()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>())).ReturnsAsync((Users)null);
            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            var dto = new UserDataDto
            {
                Name = "João",
                Email = "joao@email.com",
                Password = "123456"
            };

            await service.RegisterUser(dto);

            mockRepo.Verify(r => r.InsertAsync(It.IsAny<Users>()), Times.Once);
        }

        [Fact]
        public async Task ShouldLogSuccess()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockJwt = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>())).ReturnsAsync((Users)null);

            mockRepo.Setup(r => r.InsertAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, mockJwt.Object, mockLogger.Object);

            var dto = new UserDataDto
            {
                Name = "João",
                Email = "joao@email.com",
                Password = "123456"
            };

            await service.RegisterUser(dto);

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Usuário cadastrado com sucesso")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        #endregion

        #region LoginTests
        [Fact]
        public async Task ShouldLoginSuccessfullyWithValidCredentials()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

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
            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, options,
                                                     mockLogger.Object);

            //act
            var result = await service.UserLogin(loginDto);

            //assert
            Assert.NotNull(result);
            Assert.Contains(".", result); //verifica retorno tem estrutura de JWT
        }

        [Fact]
        public async Task ShouldExceptionWhenCredentialsAreInvalid()
        {
            //arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

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
            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, jwtOptions,
                                                     mockLogger.Object);

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
        public async Task ShouldExceptionWhenInputIsNullInLogin()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockAppSession = new Mock<IAppSession>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var jwtOptions = Options.Create(new JwtSettings { Secret = "12345678901234567890" });
            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, jwtOptions, mockLogger.Object);

            await Assert.ThrowsAsync<Exception>(() => service.UserLogin(null));
        }

        [Theory]
        [InlineData("", "123")]
        [InlineData("email@test.com", "")]
        [InlineData(" ", " ")]
        public async Task ShouldExceptionWhenFieldsAreEmpty(string email, string password)
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            var mockAppSession = new Mock<IAppSession>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var jwtOptions = Options.Create(new JwtSettings { Secret = "12345678901234567890" });

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, jwtOptions, mockLogger.Object);

            var input = new UserDataDto { Email = email, Password = password };

            var ex = await Assert.ThrowsAsync<Exception>(() => service.UserLogin(input));
            Assert.Equal("Email e senha são obrigatórios.", ex.Message);
        }

        [Fact]
        public async Task ShouldExceptionWhenUserNotFound()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync((Users)null);

            var mockAppSession = new Mock<IAppSession>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var jwtOptions = Options.Create(new JwtSettings { Secret = "12345678901234567890" });

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, jwtOptions, mockLogger.Object);

            var dto = new UserDataDto { Email = "xx@test.com", Password = "123" };

            var ex = await Assert.ThrowsAsync<Exception>(() => service.UserLogin(dto));
            Assert.Equal("Email ou senha inválidos.", ex.Message);
        }

        [Fact]
        public async Task ShouldExceptionWhenPasswordIsIncorrect()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();

            var user = new Users
            {
                Email = "joao@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("senha_correta"),
                IsDeleted = false
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(user);

            var mockAppSession = new Mock<IAppSession>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var jwtOptions = Options.Create(new JwtSettings { Secret = "12345678901234567890" });

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, jwtOptions, mockLogger.Object);

            var dto = new UserDataDto { Email = "joao@test.com", Password = "senha_errada" };

            var ex = await Assert.ThrowsAsync<Exception>(() => service.UserLogin(dto));
            Assert.Equal("Email ou senha inválidos.", ex.Message);
        }

        [Fact]
        public async Task ShouldReturnTokenWithCorrectClaims()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();

            var hashed = BCrypt.Net.BCrypt.HashPassword("123456");

            var user = new Users
            {
                Id = 55,
                Name = "Carlos",
                Email = "carlos@test.com",
                Password = hashed
            };

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(user);

            var mockAppSession = new Mock<IAppSession>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var jwtOptions = Options.Create(new JwtSettings
            {
                Secret = "12345678901234567890123456789012"
            });

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object,
                                                     jwtOptions,
                                                     mockLogger.Object);

            var dto = new UserDataDto
            {
                Email = "carlos@test.com",
                Password = "123456"
            };

            var token = await service.UserLogin(dto);

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);

            Assert.Contains(jwt.Claims, c => c.Type == "nameid" && c.Value == "55");
            Assert.Contains(jwt.Claims, c => c.Type == "unique_name" && c.Value == "Carlos");
            Assert.Contains(jwt.Claims, c => c.Type == "email" && c.Value == "carlos@test.com");
        }

        [Fact]
        public async Task ShouldLogErrorWhenLoginFails()
        {
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync((Users)null);

            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();
            var mockAppSession = new Mock<IAppSession>();
            var jwt = Options.Create(new JwtSettings { Secret = "12345678901234567890" });

            var service = new UserSettingsAppService(mockAppSession.Object, mockRepo.Object, jwt, mockLogger.Object);

            await Assert.ThrowsAsync<Exception>(() =>
                service.UserLogin(new UserDataDto { Email = "x@test.com", Password = "y" })
            );

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString().Contains("FALHA AO EFETUAR LOGIN")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
        #endregion

        #region TestsToGetUserData
        [Fact]
        public async Task ShouldReturnUserInformationsSuccessfully()
        {
            // arrange
            var user = new Users { Id = 123, Name = "John Doe", Email = "john@test.com" };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(user);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, mockJwtSettings.Object,
                                                     mockLogger.Object);

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
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, mockJwtSettings.Object,
                                                     mockLogger.Object);

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.GetUserInformations());
        }

        [Fact]
        public async Task ShouldCallRepositoryWithCorrectUserId()
        {
            // arrange
            long expectedUserId = 777;

            var mockRepo = new Mock<IGeneralRepository<Users>>();

            Expression<Func<Users, bool>> capturedPredicate = null;

            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .Callback<Expression<Func<Users, bool>>>(exp => capturedPredicate = exp)
                    .ReturnsAsync(new Users { Id = expectedUserId, Name = "Test", Email = "t@test.com" });

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(expectedUserId);

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, mockJwtSettings.Object,
                                                     mockLogger.Object);

            // act
            await service.GetUserInformations();

            // assert
            Assert.NotNull(capturedPredicate);

            var compiled = capturedPredicate.Compile();
            Assert.True(compiled(new Users { Id = expectedUserId }));
            Assert.False(compiled(new Users { Id = expectedUserId + 1 }));
        }

        [Fact]
        public async Task ShouldReturnExactDtoContent()
        {
            var user = new Users
            {
                Id = 12,
                Name = "Maria Souza",
                Email = "maria@test.com"
            };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(user);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(12);

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, mockJwtSettings.Object,
                                                     mockLogger.Object);

            // act
            var result = await service.GetUserInformations();

            // assert
            Assert.NotNull(result);
            Assert.Equal("Maria Souza", result.Name);
            Assert.Equal("maria@test.com", result.Email);
            Assert.Equal(12, result.Id);
        }
        #endregion

        #region UserDataUpdateTests        
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
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, mockJwtSettings.Object,
                                                     mockLogger.Object);

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
        public async Task ShouldExceptionWhenUserNotFoundOnUpdate()
        {
            // arrange
            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync((Users)null);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(mockAppSession.Object,
                                                     mockRepo.Object, mockJwtSettings.Object,
                                                     mockLogger.Object);

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

        [Fact]
        public async Task ShouldUpdateUserWithoutChangingPassword()
        {
            // arrange
            var existingUser = new Users
            {
                Id = 123,
                Name = "Old Name",
                Email = "old@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("oldpass")
            };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(existingUser);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(
                mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object, mockLogger.Object
            );

            var input = new UserDataForUpdateDto
            {
                Id = 123,
                Name = "New Name",
                Email = "new@test.com",
                OldPassword = null,
                NewPassword = null
            };

            // act
            await service.UpdateUserInformations(input);

            // assert
            mockRepo.Verify(r => r.UpdateAsync(It.Is<Users>(u =>
                u.Name == "New Name" &&
                u.Email == "new@test.com" &&
                u.Password == existingUser.Password  // senha não deve mudar
            )), Times.Once);
        }

        [Fact]
        public async Task ShouldExceptionWhenOldPasswordIsIncorrect()
        {
            // arrange
            var existingUser = new Users
            {
                Id = 123,
                Name = "Old",
                Email = "old@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("correctpass")
            };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(existingUser);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);
            mockAppSession.SetupProperty(x => x.EnvironmentId);

            var service = new UserSettingsAppService(
                mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object, mockLogger.Object
            );

            var input = new UserDataForUpdateDto
            {
                Id = 123,
                Name = "New",
                Email = "new@test.com",
                OldPassword = "wrongpass",
                NewPassword = "newpass"
            };

            // act & assert
            await Assert.ThrowsAsync<Exception>(() => service.UpdateUserInformations(input));
            mockRepo.Verify(r => r.UpdateAsync(It.IsAny<Users>()), Times.Never);
        }

        [Fact]
        public async Task ShouldSanitizeNameAndEmail()
        {
            // arrange
            var existingUser = new Users
            {
                Id = 123,
                Name = "Old",
                Email = "old@test.com",
                Password = BCrypt.Net.BCrypt.HashPassword("pass")
            };

            var mockRepo = new Mock<IGeneralRepository<Users>>();
            mockRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Users, bool>>>()))
                    .ReturnsAsync(existingUser);
            mockRepo.Setup(r => r.UpdateAsync(It.IsAny<Users>())).Returns(Task.CompletedTask);

            var mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            var mockLogger = new Mock<ILogger<UserSettingsAppService>>();

            var mockAppSession = new Mock<IAppSession>();
            mockAppSession.Setup(x => x.UserId).Returns(123);

            var service = new UserSettingsAppService(
                mockAppSession.Object, mockRepo.Object, mockJwtSettings.Object, mockLogger.Object
            );

            var input = new UserDataForUpdateDto
            {
                Id = 123,
                Name = "   New Name   ",
                Email = "   new@test.com   "
            };

            // act
            await service.UpdateUserInformations(input);

            // assert
            mockRepo.Verify(r => r.UpdateAsync(It.Is<Users>(u =>
                u.Name == "New Name" &&
                u.Email == "new@test.com"
            )), Times.Once);
        }
        #endregion
    }
}