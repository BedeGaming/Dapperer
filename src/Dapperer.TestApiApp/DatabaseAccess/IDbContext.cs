namespace Dapperer.TestApiApp.DatabaseAccess
{
    public interface IDbContext
    {
        ContactRepository ContactRepo { get; }
        AddressRepository AddressRepo { get; }
    }
}