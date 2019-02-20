using System.Collections.Generic;
using System.Data;
using Dapper;

namespace Dapperer.QueryBuilders.MsSql.TableValueParams
{
    public abstract class SimpleListTableValueParams<TColumnType>
    {
        private readonly IEnumerable<TColumnType> _records;
        private readonly string _tableName;
        private readonly string _columnName;

        protected SimpleListTableValueParams(
            IEnumerable<TColumnType> records,
            string tableName,
            string columnName)
        {
            _records = records;
            _tableName = tableName;
            _columnName = columnName;
        }

        public DataTable AsDataTable()
        {
            var table = new DataTable(nameof(_tableName))
            {
                Columns =
                {
                    new DataColumn(_columnName, typeof(TColumnType))
                }
            };

            foreach (var record in _records)
            {
                table.Rows.Add(record);
            }

            return table;
        }

        public SqlMapper.ICustomQueryParameter AsTableValuedParameter() =>
            AsDataTable().AsTableValuedParameter(_tableName);
    }
}