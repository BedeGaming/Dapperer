namespace Dapperer
{
    internal class ColumnInfo
    {
        internal string ColumnName { get; private set; }
        internal string FieldName { get; private set; }

        public ColumnInfo(string columnName, string fieldName)
        {
            ColumnName = columnName;
            FieldName = fieldName;
        }
    }
}