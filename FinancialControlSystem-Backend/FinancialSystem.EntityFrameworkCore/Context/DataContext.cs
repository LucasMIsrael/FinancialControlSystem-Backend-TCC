using FinancialSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FinancialSystem.EntityFrameworkCore.Context
{
    public class DataContext : DbContext
    {
        private IConfiguration _configuration;

        public DbSet<User> User { get; set; }
        public DbSet<Environments> Environments { get; set; }

        public DataContext(IConfiguration configuration, DbContextOptions options) : base(options)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var typeDatabase = _configuration["TypeDatabase"];
            var connectionString = _configuration.GetConnectionString(typeDatabase);

            optionsBuilder.UseNpgsql(connectionString);
        }
    }
}