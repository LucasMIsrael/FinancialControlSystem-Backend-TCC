using Abp.Application.Services.Dto;
using FinancialSystem.Core.Enums;

namespace FinancialSystem.Application.Shared.Dtos.Environment
{
    public class EnvironmentDataDto : EntityDto<Guid?>
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public EnvironmentTypeEnum Type { get; set; }
        public long UserId { get; set; }
    }
}
