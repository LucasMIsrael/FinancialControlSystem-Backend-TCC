using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using FinancialSystem.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialSystem.Core.Entities
{
    [Table("AppPlannedExpensesAndProfits")]
    [Audited]
    public class PlannedExpensesAndProfits : FullAuditedEntity<Guid>
    {
        public PlannedExpensesAndProfits()
        {
            CreationTime = DateTime.UtcNow;
        }

        [ForeignKey("EnvironmentId ")]
        public Environments Environment { get; set; }
        public Guid EnvironmentId { get; set; }

        public FinancialRecordTypeEnum Type { get; set; }
        public RecurrenceTypeEnum RecurrenceType { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime TransactionDate { get; set; }  //data que a transação irá ocorrer se for planejada
        public DateTime? LastProcessedDate { get; set; }  //data da ultima vez que entrou no cálculo
    }
}
