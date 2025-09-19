using FinancialSystem.Application.Shared.Interfaces;
using System.Security.Claims;

namespace FinancialSystem.Web
{
    public class AppSession : IAppSession
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AppSession(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public long? UserId
        {
            get
            {
                var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
                return claim != null ? long.Parse(claim.Value) : null;
            }
        }

        public Guid? EnvironmentId
        {
            get
            {
                var value = _httpContextAccessor.HttpContext?.Session.GetString("EnvironmentId");
                return string.IsNullOrEmpty(value) ? null : Guid.Parse(value);
            }
            set
            {
                if (value.HasValue)
                    _httpContextAccessor.HttpContext?.Session.SetString("EnvironmentId", value.Value.ToString());
                else
                    _httpContextAccessor.HttpContext?.Session.Remove("EnvironmentId");
            }
        }
    }
}
