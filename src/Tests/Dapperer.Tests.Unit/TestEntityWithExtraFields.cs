using System;

namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithExtraFields
    {
        [Column("Id", IsPrimary = true, AutoIncrement = true)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("AdditionalField1")]
        public decimal AdditionalField1 { get; set; }

        [Column("AdditionalField2")]
        public string AdditionalField2 { get; set; }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }
    }
}