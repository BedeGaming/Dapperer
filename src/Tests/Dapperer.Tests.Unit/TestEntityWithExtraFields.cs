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

        public decimal AdditionalField1 { get; set; }
        public string AdditionalField2 { get; set; }
        public DateTime AdditionalField3 { get; set; }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }
    }
}