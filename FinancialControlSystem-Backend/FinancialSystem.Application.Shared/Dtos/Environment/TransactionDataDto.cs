using Abp.Application.Services.Dto;

namespace FinancialSystem.Application.Shared.Dtos.Environment
{
    public class TransactionDataDto : EntityDto<Guid?>
    {
        public int Type { get; set; }
        public int RecurrenceType { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime TransactionDate { get; set; }  //data que a transação irá ocorrer se for planejada
    }
}
