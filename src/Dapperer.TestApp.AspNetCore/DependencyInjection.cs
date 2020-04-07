using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Dapperer.DbFactories;
using Dapperer.QueryBuilders.MsSql;
using Dapperer.TestApp.AspNetCore.DatabaseAccess;
using Microsoft.Extensions.Configuration;

namespace Dapperer.TestApp.AspNetCore
{
    public static class DependencyInjection
    {
        class DappererSettings : IDappererSettings
        {
            private readonly IConfiguration _configuration;

            public DappererSettings(IConfiguration configuration)
            {
                _configuration = configuration;
            }

            public string ConnectionString => _configuration["Dapperer.ConnectionString"];
        }

        public static void Register(ContainerBuilder builder)
        {
            builder.RegisterType<DappererSettings>().As<IDappererSettings>();
            builder.RegisterType<SqlDbFactory>().As<IDbFactory>();
            builder.RegisterType<SqlQueryBuilder>().As<IQueryBuilder>().SingleInstance();
            builder.RegisterType<DbContext>().As<IDbContext>();
        }
    }
}
