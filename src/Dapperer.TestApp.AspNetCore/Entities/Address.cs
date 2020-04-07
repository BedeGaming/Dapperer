namespace Dapperer.TestApp.AspNetCore.Entities
{
    [Table("Addresses")]
    public class Address : IIdentifier<int>
    {
        [Column("Id", IsPrimary = true, AutoIncrement = true)]
        public int Id { get; set; }

        [Column("ContactId")]
        public int ContactId { get; set; }

        [Column("Address1")]
        public string Address1 { get; set; }

        [Column("Address2")]
        public string Address2 { get; set; }

        [Column("City")]
        public string City { get; set; }

        [Column("County")]
        public string County { get; set; }

        [Column("PostCode")]
        public string PostCode { get; set; }

        public Contact Contact { get; set; }

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