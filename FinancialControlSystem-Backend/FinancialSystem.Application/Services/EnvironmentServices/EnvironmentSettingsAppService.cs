using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class EnvironmentSettingsAppService : IEnvironmentSettingsAppService
    {
        private readonly IGeneralRepository<Environments> _environmentsRepository;

        public EnvironmentSettingsAppService(IGeneralRepository<Environments> environmentsRepository)
        {
            _environmentsRepository = environmentsRepository;
        }

        public async Task InsertAndUpdateEnvironment(string desc, string name, EnvironmentTypeEnum type, Guid userId)
        {
            if (string.IsNullOrEmpty(desc) || string.IsNullOrEmpty(name))
                throw new Exception("Preencha todos os campos para continuar");
        }

        public async Task DeleteEnvironment(Guid envId)
        {
            if (envId == Guid.Empty) throw new Exception("Ambiente não encontrado para exclusão");
        }

        public async Task<List<Environments>> GetAllEnvironments()
        {
            return new List<Environments>();
        }

        public async Task<Environments> GetEnvironment(Guid envId)
        {
            if (envId == Guid.Empty) throw new Exception("Ambiente não encontrado");

            return new Environments();
        }
    }
}
