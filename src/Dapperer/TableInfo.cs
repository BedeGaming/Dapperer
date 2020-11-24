using System.Collections.Generic;

namespace Dapperer
{
    public class TableInfo : ITableInfoBase
    {
        public string TableName { get; private set; }
        public string Key { get; private set; }
        public bool KeyIsAnsi { get; private set; }
        public bool AutoIncrement { get; private set; }

        internal string InsertSql { get; private set; }
        internal string IdentityInsertSql { get; private set; }

        internal string UpdateSql { get; private set; }

        internal List<ColumnInfo> ColumnInfos { get; private set; }

        internal TableInfo(string tablename)
        {
            TableName = tablename;
            ColumnInfos = new List<ColumnInfo>();
        }

        public void AddColumnMapping(string columnName, string fieldName)
        {
            ColumnInfos.Add(new ColumnInfo(columnName, fieldName));
        }

        public void SetKey(string key)
        {
            Key = key;
        }

        public void SetKeyAnsi(bool isAnsi)
        {
            KeyIsAnsi = isAnsi;
        }

        public void SetAutoIncrement(bool autoIncrement)
        {
            AutoIncrement = autoIncrement;
        }

        public void SetInsertSql(string insertSql)
        {
            InsertSql = insertSql;
        }

        public void SetIdentityInsertSql(string insertSql)
        {
            IdentityInsertSql = insertSql;
        }

        public void SetUpdateSql(string updateSql)
        {
            UpdateSql = updateSql;
        }
    }
}