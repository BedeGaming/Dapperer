using Dapperer.DbFactories;
using Dapperer.QueryBuilders.MsSql;
using Dapperer.TestApiApp.DatabaseAccess;
using Microsoft.Extensions.DependencyInjection;

namespace Dapperer.TestApiApp
{
    public static class IoC
    {
        public static void RegisterDependencies(this IServiceCollection services)
        {
            services.AddScoped<IDappererSettings, DefaultDappererSettings>();
            services.AddScoped<IDbFactory, SqlDbFactory>();
            services.AddSingleton<IQueryBuilder, SqlQueryBuilder>();
            services.AddScoped<IDbContext, DbContext>();
        }
    }
}
