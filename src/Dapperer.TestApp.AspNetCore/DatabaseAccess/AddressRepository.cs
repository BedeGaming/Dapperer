using Dapperer.TestApp.AspNetCore.Entities;

namespace Dapperer.TestApp.AspNetCore.DatabaseAccess
{
    public class AddressRepository : Repository<Address, int>
    {
        public AddressRepository(IQueryBuilder queryBuilder, IDbFactory dbFactory) 
            : base(queryBuilder, dbFactory)
        {
        }

        public virtual void PopulateContact(Address address)
        {
            PopulateOneToOne(a => a.ContactId, a => a.Contact, address);
        }
    }
}