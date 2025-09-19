using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using FinancialSystem.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialSystem.Core.Entities
{
    [Table("AppEnvironments")]
    [Audited]
    public class Environments : FullAuditedEntity<Guid>
    {
        public Environments()
        {
            CreationTime = DateTime.UtcNow;
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public EnvironmentTypeEnum Type { get; set; }

        [ForeignKey("UserID")]
        public Users User { get; set; }
        public long UserID { get; set; }
    }
}
