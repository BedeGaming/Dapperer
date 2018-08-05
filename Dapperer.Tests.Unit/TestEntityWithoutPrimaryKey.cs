namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithoutPrimaryKey
    {
        [Column("Id", AutoIncrement = true)] // Not a primary key - IsPrimary is not set to true
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }
    }
}
