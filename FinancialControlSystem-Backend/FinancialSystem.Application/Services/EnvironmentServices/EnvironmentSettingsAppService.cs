using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class EnvironmentSettingsAppService : AppServiceBase, IEnvironmentSettingsAppService
    {
        private readonly IGeneralRepository<Environments> _environmentsRepository;

        public EnvironmentSettingsAppService(IAppSession appSession,
                                             IGeneralRepository<Environments> environmentsRepository) : base(appSession)
        {
            _environmentsRepository = environmentsRepository;
        }

        #region InsertEnvironment
        public async Task InsertEnvironment(EnvironmentDataDto input)
        {
            try
            {
                //var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

                var newEnvironment = new Environments
                {
                    Description = input.Description,
                    Id = Guid.NewGuid(),
                    Name = input.Name,
                    Type = input.Type,
                    UserID = (long)UserId
                };

                await _environmentsRepository.InsertAsync(newEnvironment);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region UpdateEnvironment
        public async Task UpdateEnvironment(EnvironmentDataDto input)
        {
            try
            {
                if (input.Id == null || input.Id == Guid.Empty)
                    throw new Exception("Identificador de ambiente não reconhecido!");

                var existingEnvironment = await _environmentsRepository.FirstOrDefaultAsync(x => x.Id == input.Id);

                if (existingEnvironment == null)
                    throw new Exception("Ambiente não encontrado!");

                existingEnvironment.Description = input.Description;
                existingEnvironment.Name = input.Name;
                existingEnvironment.Type = input.Type;

                await _environmentsRepository.UpdateAsync(existingEnvironment);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region DeleteEnvironment
        public async Task DeleteEnvironment(Guid envId)
        {
            var environment = await _environmentsRepository.FirstOrDefaultAsync(x => x.Id == envId);

            if (environment == null)
                throw new Exception("Ambiente não encontrado para exclusão");

            await _environmentsRepository.DeleteAsync(environment);
        }
        #endregion

        #region GetAllEnvironments
        public async Task<List<EnvironmentDataDto>> GetAllEnvironments()
        {
            var existingEnvironments = await _environmentsRepository.GetAll()
                                                                    .Where(x => x.UserID == (long)UserId &&
                                                                               !x.IsDeleted)
                                                                    .ToListAsync();
            if (existingEnvironments.Count == 0)
                return new List<EnvironmentDataDto>();

            var outputList = new List<EnvironmentDataDto>();

            existingEnvironments.ForEach(env =>
            {
                outputList.Add(new EnvironmentDataDto
                {
                    Description = env.Description,
                    Id = env.Id,
                    Name = env.Name,
                    Type = env.Type
                });
            });

            return outputList;
        }
        #endregion

        #region GetEnvironment
        public async Task<EnvironmentDataDto> GetEnvironment(Guid envId)
        {
            var environment = await _environmentsRepository.FirstOrDefaultAsync(x => x.Id == envId);

            if (environment == null)
                return new EnvironmentDataDto();

            return new EnvironmentDataDto
            {
                Description = environment.Description,
                Id = environment.Id,
                Name = environment.Name,
                Type = environment.Type
            };
        }
        #endregion
    }
}
