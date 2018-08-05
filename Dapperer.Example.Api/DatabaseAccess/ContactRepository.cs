using System.Data;
using System.Linq;
using Dapperer.QueryBuilders;
using Dapperer.TestApiApp.Entities;
using Dapper;

namespace Dapperer.TestApiApp.DatabaseAccess
{
    public class ContactRepository : Repository<Contact, int>
    {
        public ContactRepository(IQueryBuilder queryBuilder, IDbFactory dbFactory)
            : base(queryBuilder, dbFactory)
        {
        }

        public virtual void PopulateAddresses(Contact contact)
        {
            PopulateOneToMany<Address, int>(address => address.ContactId, c => c.Addresses, contact);
        }

        public virtual Contact GetContactByName(string name)
        {
            ITableInfoBase tableInfo = GetTableInfo();
            string sql = string.Format(@"SELECT * FROM {0} WHERE Name = @Name", tableInfo.TableName);

            using (IDbConnection connection = CreateConnection())
            {
                return connection.Query<Contact>(sql, new { Name = name }).SingleOrDefault();
            }
        }
    }
}