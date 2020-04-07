#if !NETSTANDARD
//// System.Configuration.ConfigurationManager does not work with appsettings.json

using System.Configuration;

namespace Dapperer
{

    public class DefaultDappererSettings : IDappererSettings
    {
        public string ConnectionString
        {
            get
            {
                return ConfigurationManager.AppSettings.Get("Dapperer.ConnectionString");
            }
        }
    }
}
#endif
