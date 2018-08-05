using Dapperer.QueryBuilders;

namespace Dapperer.TestApiApp.DatabaseAccess
{
    public class DbContext : IDbContext
    {
        public DbContext(IQueryBuilder queryBuilder, IDbFactory dbFactory)
        {
            ContactRepo = new ContactRepository(queryBuilder, dbFactory);
            AddressRepo = new AddressRepository(queryBuilder, dbFactory);
        }

        public ContactRepository ContactRepo { get; private set; }
        public AddressRepository AddressRepo { get; private set; }
    }
}