using System.Data;
using System.Data.SqlClient;

namespace Dapperer
{
    public class SqlDbFactory : IDbFactory
    {
        private readonly IDappererSettings _dappererSettings;

        public SqlDbFactory(IDappererSettings dappererSettings)
        {
            _dappererSettings = dappererSettings;
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_dappererSettings.ConnectionString);
        }
    }
}
