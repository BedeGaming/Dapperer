using Dapperer.QueryBuilders;
using Dapperer.TestApiApp.Entities;

namespace Dapperer.TestApiApp.DatabaseAccess
{
    public class ContactRepository : Repository<Contact, int>
    {
        public ContactRepository(IQueryBuilder queryBuilder, IDbFactory dbFactory)
            : base(queryBuilder, dbFactory)
        {
        }

        public void PopulateAddresses(Contact contact)
        {
            Populate<Address, int>(address => address.ContactId, c => c.Addresses, contact);
        }
    }
}