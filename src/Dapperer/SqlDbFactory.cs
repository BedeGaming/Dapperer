using System.Data;
using System.Data.SqlClient;

namespace Dapperer
{
    public class SqlDbFactory : IDbFactory
    {
        public IDbConnection CreateConnection()
        {
            return new SqlConnection();
        }
    }
}
