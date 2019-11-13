using System;
using Dapperer;

namespace Dapperer.Tests.Unit
{
    [Table("TestTable")]
    public class TestEntityWithExtraFields : IIdentifier<int>
    {
        [Column("Id", IsPrimary = true, AutoIncrement = true)]
        public int Id { get; set; }

        [Column("Name")]
        public string Name { get; set; }

        [Column("AdditionalField1")]
        public decimal AdditionalField1 { get; set; }

        [Column("AdditionalField2")]
        public string AdditionalField2 { get; set; }

        public int GetIdentity()
        {
            return Id;
        }

        public void SetIdentity(int identity)
        {
            Id = identity;
        }
    }
}