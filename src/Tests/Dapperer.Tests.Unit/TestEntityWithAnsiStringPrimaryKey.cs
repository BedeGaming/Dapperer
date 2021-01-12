namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithAnsiStringPrimaryKey
    {
        [Column("Id", IsPrimary = true, IsAnsi = true)]
        public string Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public void SetIdentity(string identity)
        {
            Id = identity;
        }
    }
}