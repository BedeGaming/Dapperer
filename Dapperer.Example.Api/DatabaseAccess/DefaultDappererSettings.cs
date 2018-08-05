using Microsoft.Extensions.Configuration;

namespace Dapperer.TestApiApp.DatabaseAccess
{
    public class DefaultDappererSettings : IDappererSettings
    {
        private readonly IConfiguration _configuration;

        public DefaultDappererSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string ConnectionString => _configuration.GetValue<string>("Dapperer.ConnectionString");
    }
}
