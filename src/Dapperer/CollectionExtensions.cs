using System.Collections.Generic;
using System.Linq;

namespace Dapperer
{
    public static class CollectionExtensions
    {
        public static bool IsNullOrEmpty<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                return true;
            }

            var collection = source as ICollection<TSource>;
            if (collection != null)
            {
                return collection.Count < 1;
            }

            return !source.Any();
        }
    }
}
