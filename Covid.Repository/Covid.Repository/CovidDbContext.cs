using Covid.Repository.Model.Users;
using System.Data.Entity;

namespace Covid.Repository
{
    public class CovidDbContext : DbContext, ICovidDbContext
    {
        public virtual DbSet<User> Users { get; set; }

        public CovidDbContext(string connectionString)
            : base(connectionString)
        {
        }

        private CovidDbContext()
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }

        public void SetValues<TEntity>(TEntity existingEntity, TEntity updatedEntity) where TEntity : class
        {
            Entry(existingEntity).CurrentValues.SetValues(updatedEntity);
        }

        public void SetModified<TEntity>(TEntity existingEntity) where TEntity : class
        {
            Entry(existingEntity).State = EntityState.Modified;
        }
    }
}
