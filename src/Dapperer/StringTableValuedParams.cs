using System.Collections.Generic;
using Microsoft.SqlServer.Server;

namespace Dapperer
{
    public class StringTableValuedParams : TableValuedParams
    {
        private readonly IEnumerable<string> _items;

        public StringTableValuedParams(string tableValuedParam, IEnumerable<string> items)
            : base(tableValuedParam, "StringList")
        {
            _items = items;
        }

        protected override List<SqlDataRecord> GenerateTableParameterRecords()
        {
            return GenerateStringTableParameterRecords(_items);
        }
    }
}
