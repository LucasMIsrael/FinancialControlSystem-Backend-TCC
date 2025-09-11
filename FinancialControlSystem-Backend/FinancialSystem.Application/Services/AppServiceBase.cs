using FinancialSystem.Application.Shared.Interfaces;

namespace FinancialSystem.Application.Services
{
    public abstract class AppServiceBase
    {
        protected readonly IAppSession AppSession;

        protected AppServiceBase(IAppSession appSession)
        {
            AppSession = appSession;
        }

        protected Guid? EnvironmentId => AppSession.EnvironmentId;
        protected long? UserId => AppSession.UserId;
    }
}