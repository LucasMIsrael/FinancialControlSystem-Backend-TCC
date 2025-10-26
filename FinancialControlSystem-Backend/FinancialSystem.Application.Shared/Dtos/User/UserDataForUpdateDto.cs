using Abp.Application.Services.Dto;

namespace FinancialSystem.Application.Shared.Dtos.User
{
    public class UserDataForUpdateDto : EntityDto<long?>
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string? OldPassword { get; set; }
        public string? NewPassword { get; set; }
    }
}