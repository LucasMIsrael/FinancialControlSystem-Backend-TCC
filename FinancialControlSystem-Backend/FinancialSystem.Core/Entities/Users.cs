using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialSystem.Core.Entities
{
    [Table("AppUsers")]
    [Audited]
    public class Users : FullAuditedEntity<long>
    {
        public Users()
        {
            CreationTime = DateTime.UtcNow.AddHours(-3);
        }

        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
