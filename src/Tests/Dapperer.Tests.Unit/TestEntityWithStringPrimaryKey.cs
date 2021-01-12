namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithStringPrimaryKey
    {
        [Column("Id", IsPrimary = true, IsAnsi = false)]
        public string Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        public void SetIdentity(string identity)
        {
            Id = identity;
        }
    }
}