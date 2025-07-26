using FinancialSystem.Application.Shared.Dtos.User;
using FinancialSystem.Application.Shared.Interfaces.UserSettings;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;

namespace FinancialSystem.Application.Services.UserSettings
{
    public class UserSettingsAppService : IUserSettingsAppService
    {
        private readonly IGeneralRepository<User> _userRepository;

        public UserSettingsAppService(IGeneralRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task RegisterAsync(UserDataDto input)
        {
            if (input == null)
                throw new Exception("sem informações de cadastro");

            var existingUser = await _userRepository.FirstOrDefaultAsync(x => x.Email == input.Email);

            if (existingUser != null)
                throw new Exception("Já existe um usuário cadastrado com este email");

            var userData = new User
            {
                Id = Guid.NewGuid(),
                Email = input.Email,
                Name = input.Name,
                Password = input.Password
            };

            await _userRepository.InsertAsync(userData);
        }

        public async Task<User?> LoginAsync(string email, string password)
        {
            return await _userRepository.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
        }
    }
}
