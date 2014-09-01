using System;
using System.Web.Http;

namespace Dapperer.TestApiApp
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(WebApiRoutes.Register);
            GlobalConfiguration.Configure(DependencyInjection.Register);
        }
    }
}