using FinancialSystem.Application.Shared.Dtos.Environment;
using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using FinancialSystem.Core.Entities;
using FinancialSystem.Core.Enums;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class RankingAppService : AppServiceBase, IRankingAppService
    {
        private readonly IGeneralRepository<Environments> _environmentsRepository;
        private TimeZoneInfo _tzBrasilia = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");

        public RankingAppService(IAppSession appSession,
                                 IGeneralRepository<Environments> environmentsRepository) : base(appSession)
        {
            _environmentsRepository = environmentsRepository;
        }

        #region GetRankingByAmbienteAsync
        public async Task<List<RankingDto>> GetEnvironmentsForRanking()
        {
            var currentEnvironment = await _environmentsRepository.GetByIdAsync((Guid)EnvironmentId);

            if (currentEnvironment == null)
                throw new Exception("Ambiente atual não encontrado");

            var otherEnvironments = await _environmentsRepository
                                          .GetAll().Include(x => x.User)
                                          .Where(x => x.Type == currentEnvironment.Type &&
                                                     !x.IsDeleted)
                                          .ToListAsync();

            var ranking = new List<RankingDto>();

            foreach (var env in otherEnvironments)
            {
                ranking.Add(new RankingDto
                {
                    UserName = env.User?.Name ?? "Usuário desconhecido",
                    TotalGoalsAchieved = env.TotalGoalsAchieved,
                    EnvironmentLevel = GetLevelNamePt(env.FinancialControlLevel),
                    CreationTime = TimeZoneInfo.ConvertTime(env.CreationTime, _tzBrasilia).ToShortDateString()
                });
            }

            return ranking.OrderByDescending(r => r.TotalGoalsAchieved)
                          .Take(10)
                          .ToList();
        }
        #endregion

        #region GetLevelNamePt
        private string GetLevelNamePt(FinancialControlLevelEnum level)
        {
            return level switch
            {
                FinancialControlLevelEnum.None => string.Empty,
                FinancialControlLevelEnum.Beginner => "Iniciante",
                FinancialControlLevelEnum.Learning => "Aprendendo",
                FinancialControlLevelEnum.Intermediate => "Intermediário",
                FinancialControlLevelEnum.Advanced => "Avançado",
                FinancialControlLevelEnum.Expert => "Especialista",
                FinancialControlLevelEnum.Master => "Mestre",
                FinancialControlLevelEnum.FinancialController => "Controlador",
                _ => string.Empty
            };
        }
        #endregion
    }
}