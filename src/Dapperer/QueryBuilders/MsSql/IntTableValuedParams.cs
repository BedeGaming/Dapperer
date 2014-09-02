using System.Collections.Generic;
using Microsoft.SqlServer.Server;

namespace Dapperer.QueryBuilders.MsSql
{
    public class IntTableValuedParams : TableValuedParams
    {
        private readonly IEnumerable<int> _items;

        public IntTableValuedParams(string tableValuedParam, IEnumerable<int> items)
            : base(tableValuedParam, "IntList")
        {
            _items = items;
        }

        protected override List<SqlDataRecord> GenerateTableParameterRecords()
        {
            return GenerateIntTableParameterRecords(_items);
        }
    }
}
