using Abp.Application.Services.Dto;

namespace FinancialSystem.Application.Shared.Dtos.User
{
    public class UserInfoForViewDto : EntityDto<long?>
    {
        public string Name { get; set; }
        public string Email { get; set; }
    }
}
