using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using FinancialSystem.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialSystem.Core.Entities
{
    [Table("AppGoals")]
    [Audited]
    public class Goals : FullAuditedEntity<Guid>
    {
        public Goals()
        {
            CreationTime = DateTime.UtcNow.AddHours(-3);
        }

        [ForeignKey("EnvironmentId")]
        public Environments Environment { get; set; }
        public Guid EnvironmentId { get; set; }

        public int GoalNumber { get; set; }
        public string Description { get; set; }
        public double Value { get; set; }
        public bool? Status { get; set; }
        public GoalPeriodTypeEnum? PeriodType { get; set; }
        public DateTime? StartDate { get; set; }            //data que irá começar a valer a meta quando for recorrente
        public DateTime? SingleDate { get; set; }           //data limite da Meta se não for recorrente
        public DateTime? LastEvaluatedDate { get; set; }    //última vez que a meta foi verificada
        public int AchievementsCount { get; set; } = 0;     //quantas vezes já foi alcançada
    }
}