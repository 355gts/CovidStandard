using Covid.Repository.Model.Users;
using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading;
using System.Threading.Tasks;

namespace Covid.Repository
{
    public interface ICovidDbContext : IDisposable
    {
        DbSet<User> Users { get; set; }

        Task<int> SaveChangesAsync();

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken));

        void SetValues<TEntity>(TEntity existingEntity, TEntity updatedEntity) where TEntity : class;

        void SetModified<TEntity>(TEntity existingEntity) where TEntity : class;

        DbEntityEntry Entry(object entity);

        DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
    }
}
