namespace Dapperer.TestApp.AspNetCore.DatabaseAccess
{
    public interface IDbContext
    {
        ContactRepository ContactRepo { get; }
        AddressRepository AddressRepo { get; }
    }
}