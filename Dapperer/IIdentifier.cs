namespace Dapperer
{
    public interface IIdentifier<TPrimaryKey>
    {
        void SetIdentity(TPrimaryKey identity);
        TPrimaryKey GetIdentity();
    }
}
