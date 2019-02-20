namespace Dapperer
{
    public interface ITableInfoBase
    {
        string TableName { get;}
        string Key { get;}
        bool AutoIncrement { get;}
    }
}
