namespace Dapperer.Tests.Unit
{
    public class TestEntityNoTableSpecified
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
