using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces.UserServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Settings;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FinancialSystem.Application.Services.UserServices
{
    public class UserSettingsAppService : IUserSettingsAppService
    {
        private readonly IGeneralRepository<User> _userRepository;
        private readonly JwtSettings _jwtSettings;

        public UserSettingsAppService(IGeneralRepository<User> userRepository,
                                      IOptions<JwtSettings> jwtOptions)
        {
            _userRepository = userRepository;
            _jwtSettings = jwtOptions.Value;
        }

        #region RegisterUser
        public async Task RegisterUser(UserDataDto input)
        {
            if (input == null)
                throw new Exception("sem informações de cadastro");

            var existingUser = await _userRepository.FirstOrDefaultAsync(x => x.Email == input.Email);

            if (existingUser != null)
                throw new Exception("Já existe um usuário cadastrado com este email");

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(input.Password);

            var userData = new User
            {
                Id = Guid.NewGuid(),
                Email = input.Email,
                Name = input.Name,
                Password = hashedPassword
            };

            await _userRepository.InsertAsync(userData);
        }
        #endregion

        #region UserLogin
        public async Task<string> UserLogin(UserDataDto input)
        {
            try
            {
                var user = await _userRepository.FirstOrDefaultAsync(x => x.Email == input.Email &&
                                                                         !x.IsDeleted);

                if (user == null || !BCrypt.Net.BCrypt.Verify(input.Password, user.Password))
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
                return tokenHandler.WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}
