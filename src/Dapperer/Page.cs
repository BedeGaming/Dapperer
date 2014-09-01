using System.Collections.Generic;

namespace Dapperer
{
    public class Page<TEntity>
    {
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public List<TEntity> Items { get; set; }
    }
}
