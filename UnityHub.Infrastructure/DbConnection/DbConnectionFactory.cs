using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace UnityHub.Infrastructure.DbConnection
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
        }

        public SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }
        public string ConnectionString => _connectionString;
    }
}
