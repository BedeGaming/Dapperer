using System;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Dapperer.DbFactories;
using Dapperer.QueryBuilders;
using Dapperer.QueryBuilders.MsSql;
using Dapperer.TestApiApp.Controllers;
using Dapperer.TestApiApp.DatabaseAccess;

namespace Dapperer.TestApiApp
{
    public static class DependencyInjection
    {
        public static void Register(HttpConfiguration configuration)
        {
            Register(configuration, null);
        }

        public static void Register(HttpConfiguration configuration, Action<ContainerBuilder> bindingOverride)
        {
            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(typeof(ContactsController).Assembly);
            builder.RegisterType<DefaultDappererSettings>().As<IDappererSettings>();
            builder.RegisterType<SqlDbFactory>().As<IDbFactory>();
            builder.RegisterType<SqlQueryBuilder>().As<IQueryBuilder>().SingleInstance();
            builder.RegisterType<DbContext>().As<IDbContext>();

            IContainer container = builder.Build();

            // Set the dependency resolver implementation.
            configuration.DependencyResolver = new AutofacWebApiDependencyResolver(container);
        }
    }
}