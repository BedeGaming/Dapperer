using System.Collections.Generic;

namespace Dapperer
{
    public class TableInfo : ITableInfoBase
    {
        public string TableName { get; }
        public string Key { get; private set; }
        public bool AutoIncrement { get; private set; }

        internal string InsertSql { get; private set; }
        internal string UpdateSql { get; private set; }

        internal List<ColumnInfo> ColumnInfos { get; }

        internal TableInfo(string tablename)
        {
            TableName = tablename;
            ColumnInfos = new List<ColumnInfo>();
        }

        public void AddColumnMapping(string columnName, string fieldName) =>
            ColumnInfos.Add(new ColumnInfo(columnName, fieldName));

        public void SetKey(string key) => Key = key;

        public void SetAutoIncrement(bool autoIncrement) => AutoIncrement = autoIncrement;

        public void SetInsertSql(string insertSql) => InsertSql = insertSql;

        public void SetUpdateSql(string updateSql) => UpdateSql = updateSql;
    }
}