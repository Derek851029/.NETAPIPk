using MySql.Data.MySqlClient;
using System.Data;

namespace PKApp.DIObject
{
    public class DapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionStringCMS;
        private readonly string _connectionStringWebActivity;

        public DapperContext(IConfiguration configuration)
        {
            string? env = Environment.GetEnvironmentVariable("stage");

            if (env == null)
            {
                _configuration = configuration;
                _connectionStringCMS = _configuration.GetConnectionString("SqlConnectionCMS_dev");
                _connectionStringWebActivity = _configuration.GetConnectionString("SqlConnectionWebActivity_dev");
            }
            else
            {
                _configuration = configuration;
                _connectionStringCMS = _configuration.GetConnectionString("SqlConnectionCMS_" + env);
                _connectionStringWebActivity = _configuration.GetConnectionString("SqlConnectionWebActivity_" + env);
            }

        }
        public IDbConnection CreateConnection()
            => new MySqlConnection(_connectionStringCMS);
        public IDbConnection CreateConnection2()
            => new MySqlConnection(_connectionStringWebActivity);
        //=> new SqlConnection(_connectionString); //MSSQL Connection
    }
}
