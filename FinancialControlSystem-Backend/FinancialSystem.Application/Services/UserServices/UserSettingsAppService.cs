using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.UserServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace FinancialSystem.Application.Services.UserServices
{
    public class UserSettingsAppService : AppServiceBase, IUserSettingsAppService
    {
        private readonly IGeneralRepository<Users> _usersRepository;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<UserSettingsAppService> _logger;

        public UserSettingsAppService(IAppSession appSession,
                                      IGeneralRepository<Users> usersRepository,
                                      IOptions<JwtSettings> jwtOptions,
                                      ILogger<UserSettingsAppService> logger) : base(appSession)
        {
            _usersRepository = usersRepository;
            _jwtSettings = jwtOptions.Value;
            _logger = logger;
        }

        #region RegisterUser
        public async Task RegisterUser(UserDataDto input)
        {
            if (input == null)
                throw new Exception("sem informações de cadastro");

            var existingUser = await _usersRepository.FirstOrDefaultAsync(x => x.Email == input.Email);

            if (existingUser != null)
                throw new Exception("Já existe um usuário cadastrado com este email");

            input.Email = Sanitize(input.Email);
            input.Name = Sanitize(input.Name);
            input.Password = Sanitize(input.Password);

            if (string.IsNullOrEmpty(input.Email) ||
                string.IsNullOrEmpty(input.Name) ||
                string.IsNullOrEmpty(input.Password))
                throw new Exception("Campo obrigatório com valor inválido!");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(input.Password);

            var userData = new Users
            {
                Email = input.Email,
                Name = input.Name,
                Password = hashedPassword
            };

            await _usersRepository.InsertAsync(userData);
            _logger.LogInformation($"Usuário cadastrado com sucesso: {userData.Id} - {userData.Name}");
        }
        #endregion

        #region UserLogin
        public async Task<string> UserLogin(UserDataDto input)
        {
            try
            {
                var email = Sanitize(input.Email);
                var password = Sanitize(input.Password);

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                    throw new Exception("Email e senha são obrigatórios.");

                var user = await _usersRepository.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

                if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
                    throw new Exception("Email ou senha inválidos.");

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSettings.Secret);

                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Name, user.Name),
                        new Claim(ClaimTypes.Email, user.Email)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);

                _logger.LogInformation($"Login efetuado - Usuário: {user.Id} - {user.Name}");
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"FALHA AO EFETUAR LOGIN: {ex.Message}");
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region GetUserInformations
        public async Task<UserInfoForViewDto> GetUserInformations()
        {
            var user = await _usersRepository.FirstOrDefaultAsync(x => x.Id == (long)UserId);

            if (user == null)
                throw new Exception("Erro ao consultar dados de usuário");

            return new UserInfoForViewDto
            {
                Email = user.Email,
                Name = user.Name,
                Id = user.Id
            };
        }
        #endregion

        #region UpdateUserInformations
        public async Task UpdateUserInformations(UserDataForUpdateDto input)
        {
            var user = await _usersRepository.FirstOrDefaultAsync(x => x.Id == input.Id);

            if (user == null)
                throw new Exception("Erro ao consultar usuário");

            user.Name = Sanitize(input.Name);
            user.Email = Sanitize(input.Email);

            if (!string.IsNullOrEmpty(input.OldPassword) &&
                !string.IsNullOrEmpty(input.NewPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(input.OldPassword, user.Password))
                    throw new Exception("Senha atual inválida");

                var hashedNewPassword = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);

                user.Password = hashedNewPassword;
            }

            await _usersRepository.UpdateAsync(user);
        }
        #endregion

        #region Sanitize
        private string Sanitize(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            input = Regex.Replace(input, @"<.*?>", string.Empty);
            input = Regex.Replace(input, @"[()<>""'%;+]", string.Empty);

            return input.Trim();
        }
        #endregion
    }
}