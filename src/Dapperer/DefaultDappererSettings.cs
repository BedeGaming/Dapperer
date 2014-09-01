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
