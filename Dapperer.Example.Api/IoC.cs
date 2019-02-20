using Dapperer.DbFactories;
using Dapperer.Example.Api.DatabaseAccess;
using Dapperer.QueryBuilders.MsSql;
using Microsoft.Extensions.DependencyInjection;

namespace Dapperer.Example.Api
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
