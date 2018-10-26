using System.Collections.Generic;

namespace Dapperer.QueryBuilders.MsSql.TableValueParams
{
    public class StringList : SimpleListTableValueParams<string>
    {
        public StringList(IEnumerable<string> records) : base(records, "StringList", "Id")
        {
        }
    }
}
