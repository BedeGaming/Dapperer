namespace Dapperer.Example.Api.DatabaseAccess
{
    public interface IDbContext
    {
        ContactRepository ContactRepo { get; }
        AddressRepository AddressRepo { get; }
    }
}