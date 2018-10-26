using System.Collections.Generic;

namespace Dapperer.QueryBuilders.MsSql.TableValueParams
{
    public class IntList : SimpleListTableValueParams<int>
    {
        public IntList(IEnumerable<int> records) : base(records, "IntList", "Id")
        {
        }
    }
}
