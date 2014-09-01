using System.Data;

namespace Dapperer
{
    public interface IDbFactory
    {
        IDbConnection CreateConnection();
    }
}
