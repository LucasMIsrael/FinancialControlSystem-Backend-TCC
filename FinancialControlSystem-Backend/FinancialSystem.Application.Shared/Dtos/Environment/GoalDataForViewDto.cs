using Abp.Application.Services.Dto;
using FinancialSystem.Core.Enums;

namespace FinancialSystem.Application.Shared.Dtos.Environment
{
    public class GoalDataForViewDto : EntityDto<Guid?>
    {
        public int GoalNumber { get; set; }
        public string Description { get; set; }
        public double Value { get; set; }
        public bool? Status { get; set; }
        public GoalPeriodTypeEnum? PeriodType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? SingleDate { get; set; }
    }
}
