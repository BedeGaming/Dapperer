using System.Collections.Generic;

namespace Dapperer.QueryBuilders.MsSql.TableValueParams
{
    public class LongList : SimpleListTableValueParams<long>
    {
        public LongList(IEnumerable<long> records) : base(records, "LongList", "Id")
        {
        }
    }
}
