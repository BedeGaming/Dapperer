namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithoutAutoIncreamentId
    {
        [Column("Id", IsPrimary = true, AutoIncrement = false)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }
    }
}
