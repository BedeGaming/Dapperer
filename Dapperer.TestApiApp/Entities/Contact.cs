using System.Collections.Generic;

namespace Dapperer.TestApiApp.Entities
{
    [Table("Contacts")]
    public class Contact : IIdentifier<int>
    {
        [Column("Id", IsPrimary = true, AutoIncrement = true)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public List<Address> Addresses { get; set; }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }

        public int GetIdentity()
        {
            return Id;
        }
    }
}