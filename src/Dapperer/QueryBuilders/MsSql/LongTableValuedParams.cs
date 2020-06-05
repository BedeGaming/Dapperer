using Microsoft.Data.SqlClient.Server;
using System.Collections.Generic;

namespace Dapperer.QueryBuilders.MsSql
{
    public class LongTableValuedParams : TableValuedParams
    {
        private readonly IEnumerable<long> _items;

        public LongTableValuedParams(string tableValuedParam, IEnumerable<long> items)
            : base(tableValuedParam, "LongList")
        {
            _items = items;
        }

        protected override List<SqlDataRecord> GenerateTableParameterRecords()
        {
            return GenerateLongTableParameterRecords(_items);
        }
    }
}
