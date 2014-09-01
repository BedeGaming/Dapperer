using System.Web.Http;

namespace Dapperer.TestApiApp
{
    public static class WebApiRoutes
    {
        public static void Register(HttpConfiguration config)
        {
            // All routes via attribute routing
            config.MapHttpAttributeRoutes();
        }
    }
}