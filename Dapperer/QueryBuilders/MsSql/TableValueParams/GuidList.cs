using System;
using System.Collections.Generic;

namespace Dapperer.QueryBuilders.MsSql.TableValueParams
{
    public class GuidList : SimpleListTableValueParams<Guid>
    {
        public GuidList(IEnumerable<Guid> records) : base(records, "GuildList", "Id")
        {
        }
    }
}
