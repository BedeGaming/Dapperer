using Dapperer.QueryBuilders;
using Dapperer.TestApiApp.Entities;

namespace Dapperer.TestApiApp.DatabaseAccess
{
    public class AddressRepository : Repository<Address, int>
    {
        public AddressRepository(IQueryBuilder queryBuilder, IDbFactory dbFactory) 
            : base(queryBuilder, dbFactory)
        {
        }

        public virtual void PopulateContact(Address address)
        {
            Populate(a => a.ContactId, a => a.Contact, address);
        }
    }
}