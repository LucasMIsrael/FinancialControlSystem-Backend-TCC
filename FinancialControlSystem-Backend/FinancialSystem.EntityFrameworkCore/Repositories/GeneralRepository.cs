using FinancialSystem.EntityFrameworkCore.Context;
using FinancialSystem.EntityFrameworkCore.Repositories.RepositoryInterfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FinancialSystem.EntityFrameworkCore.Repositories
{
    public class GeneralRepository<TEntity> : IGeneralRepository<TEntity> where TEntity : class
    {
        private readonly DataContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public GeneralRepository(DataContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
        }

        public async Task<TEntity?> GetByIdAsync(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public IQueryable<TEntity> GetAll()
        {
            return _dbSet.AsQueryable();
        }

        public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public TEntity? FirstOrDefault(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.FirstOrDefault(predicate);
        }

        public async Task<TEntity?> FirstOrDefaultAsync()
        {
            return await _dbSet.FirstOrDefaultAsync();
        }

        public TEntity? FirstOrDefault()
        {
            return _dbSet.FirstOrDefault();
        }

        public async Task InsertAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(TEntity entity)
        {
            var dataProperty = typeof(TEntity).GetProperty("LastModificationTime");
            if (dataProperty != null)
            {
                dataProperty.SetValue(entity, DateTime.UtcNow);
            }

            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(TEntity entity)
        {
            var property = typeof(TEntity).GetProperty("IsDeleted");
            if (property != null && property.PropertyType == typeof(bool))
            {
                property.SetValue(entity, true);

                var dataProperty = typeof(TEntity).GetProperty("DeletionTime");

                if (dataProperty != null)
                    dataProperty.SetValue(entity, DateTime.UtcNow);

                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }

            await _context.SaveChangesAsync();
        }

        public void Insert(TEntity entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public void Update(TEntity entity)
        {
            var dataProperty = typeof(TEntity).GetProperty("LastModificationTime");
            if (dataProperty != null)
            {
                dataProperty.SetValue(entity, DateTime.UtcNow);
            }

            _dbSet.Update(entity);
            _context.SaveChanges();
        }

        public void Delete(TEntity entity)
        {
            var property = typeof(TEntity).GetProperty("IsDeleted");
            if (property != null && property.PropertyType == typeof(bool))
            {
                property.SetValue(entity, true);                

                var dataProperty = typeof(TEntity).GetProperty("DeletionTime");

                if (dataProperty != null)
                    dataProperty.SetValue(entity, DateTime.UtcNow);

                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }            

            _context.SaveChanges();
        }
    }
}
