namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithAutoIncreamentId
    {
        [Column("Id", IsPrimary = true, AutoIncrement = true)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }
    }
}