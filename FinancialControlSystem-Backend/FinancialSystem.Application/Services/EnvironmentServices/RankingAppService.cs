using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class RankingAppService : AppServiceBase, IRankingAppService
    {
        private readonly IGeneralRepository<Environments> _environmentsRepository;

        public RankingAppService(IAppSession appSession,
                                 IGeneralRepository<Environments> environmentsRepository) : base(appSession)
        {
            _environmentsRepository = environmentsRepository;
        }
    }
}
