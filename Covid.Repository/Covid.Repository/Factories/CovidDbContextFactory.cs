using System;

namespace Covid.Repository.Factories
{
    public class CovidDbContextFactory : ICovidDbContextFactory
    {
        private readonly string _connectionString;

        public CovidDbContextFactory(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public ICovidDbContext GetContext()
        {
            // todo pass username through in request
            return new CovidDbContext(_connectionString);
        }
    }
}
