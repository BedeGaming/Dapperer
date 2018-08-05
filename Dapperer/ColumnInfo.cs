namespace Dapperer
{
    internal class ColumnInfo
    {
        internal string ColumnName { get; }
        internal string FieldName { get; }

        public ColumnInfo(string columnName, string fieldName)
        {
            ColumnName = columnName;
            FieldName = fieldName;
        }
    }
}