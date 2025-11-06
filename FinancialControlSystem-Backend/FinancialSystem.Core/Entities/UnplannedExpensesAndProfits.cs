using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using FinancialSystem.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialSystem.Core.Entities
{
    [Table("AppUnplannedExpensesAndProfits")]
    [Audited]
    public class UnplannedExpensesAndProfits : FullAuditedEntity<Guid>
    {
        public UnplannedExpensesAndProfits()
        {
            CreationTime = DateTime.UtcNow.AddHours(-3);
        }

        [ForeignKey("EnvironmentId ")]
        public Environments Environment { get; set; }
        public Guid EnvironmentId { get; set; }

        public FinancialRecordTypeEnum Type { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime TransactionDate { get; set; }  //data que a transação ocorreu
        public DateTime? LastProcessedDate { get; set; }  // data da última vez que entrou no cálculo
    }
}