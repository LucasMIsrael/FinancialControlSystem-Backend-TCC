using Abp.Auditing;
using Abp.Domain.Entities.Auditing;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinancialSystem.Core.Entities
{
    [Table("AppUser")]
    [Audited]
    public class User : FullAuditedEntity<Guid>
    {
        public User()
        {
            CreationTime = DateTime.UtcNow;
        }

        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
    }
}