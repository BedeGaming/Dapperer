using System;
using System.Linq;
using System.Reflection;
using Dapper;

namespace Dapperer
{
    public static class CustomPropertyExtensions
    {
        /// <summary>
        /// Use <see cref="ColumnAttribute"/> to map SQL queries to entities
        /// Look for all entities with <see cref="TableAttribute"/> in the requested <param name="assembly"></param>
        /// and set type mapping for them
        /// </summary>
        /// <param name="assembly">Assembly that contains POCO entity classes</param>
        public static void UseDappererColumnMapping(this Assembly assembly)
        {
            foreach (var type in assembly.GetTypes().Where(HasTableEntities))
            {
                SetTypeMap(type);
            }
        }

        private static bool HasTableEntities(Type type) =>
            type.GetCustomAttributes(typeof(TableAttribute), false).Length > 0;

        private static void SetTypeMap(Type entityType) =>
            SqlMapper.SetTypeMap(entityType, new CustomPropertyTypeMap(entityType, PropertySelector));

        private static readonly Func<Type, string, PropertyInfo> PropertySelector = (type, columnName) =>
            type.GetProperties().FirstOrDefault(prop =>
                prop.GetCustomAttributes(false)
                    .OfType<ColumnAttribute>()
                    .Any(attr => attr.Name == columnName));
    }
}
