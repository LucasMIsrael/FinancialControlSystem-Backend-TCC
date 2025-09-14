using FinancialSystem.Application.Shared.Interfaces;
using FinancialSystem.Application.Shared.Interfaces.EnvironmentServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialSystem.Application.Services.EnvironmentServices
{
    public class GoalsSettingsAppService : AppServiceBase, IGoalsSettingsAppService
    {


        public GoalsSettingsAppService(IAppSession appSession) : base(appSession)
        {
        }
    }
}
