using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dapperer.QueryBuilders.MsSql
{
    /// <summary>
    /// Builds the basic CRUD queries for an entity
    /// Cache table info and CRUD queries in memory - create a singleton instance per life time
    /// </summary>
    public class SqlQueryBuilder : IQueryBuilder
    {
        private readonly Dictionary<Type, TableInfo> _tableInfos;

        public SqlQueryBuilder()
        {
            _tableInfos = new Dictionary<Type, TableInfo>();
        }

        public string GetByPrimaryKeyQuery<TEntity>()
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            return $"SELECT * FROM {tableInfo.TableName} WHERE {tableInfo.Key} = @Key";
        }

        public string GetByPrimaryKeysQuery<TEntity>()
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            return $"SELECT * FROM {tableInfo.TableName} WHERE {tableInfo.Key} IN @Keys";
        }

        public string GetAll<TEntity>()
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();
            return $"SELECT * FROM {tableInfo.TableName}";
        }

        public PagingSql PageQuery<TEntity>(long skip, long take, string orderByQuery = null, string filterQuery = null)
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();
            var tableName = tableInfo.TableName;
            var primaryKey = tableInfo.Key;

            if (string.IsNullOrWhiteSpace(orderByQuery))
            {
                orderByQuery = $"ORDER BY {primaryKey}";
            }

            var pagingSql = new PagingSql();
            if (string.IsNullOrWhiteSpace(filterQuery))
            {
                pagingSql.Items = $"SELECT * FROM {tableName} {orderByQuery} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
                pagingSql.Count = $"SELECT CAST(COUNT(*) AS Int) AS total FROM {tableName}";
            }
            else
            {
                pagingSql.Items = $"SELECT DISTINCT {tableName}.* FROM {tableName} {filterQuery} {orderByQuery} OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY";
                pagingSql.Count = $"SELECT CAST(COUNT(DISTINCT {tableName}.{primaryKey}) AS Int) AS total FROM {tableName} {filterQuery}";
            }

            return pagingSql;
        }

        public string InsertQuery<TEntity, TPrimaryKey>()
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();

            lock (tableInfo)
            {
                if (string.IsNullOrWhiteSpace(tableInfo.InsertSql))
                {
                    CacheInsertSql(tableInfo);
                }
            }

            return tableInfo.InsertSql;
        }

        public string UpdateQuery<TEntity>()
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();

            lock (tableInfo)
            {
                if (string.IsNullOrWhiteSpace(tableInfo.UpdateSql))
                {
                    CacheUpdateSql(tableInfo);
                }
            }

            return tableInfo.UpdateSql;
        }

        public string DeleteQuery<TEntity>()
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            return DeleteQuery<TEntity>($"WHERE {tableInfo.Key} = @Key");
        }

        public string DeleteQuery<TEntity>(string filterQuery)
            where TEntity : class
        {
            var tableInfo = GetTableInfo<TEntity>();

            return $"DELETE FROM {tableInfo.TableName} {filterQuery}";
        }

        public ITableInfoBase GetBaseTableInfo<TEntity>()
            where TEntity : class
        {
            return GetTableInfo<TEntity>();
        }

        private TableInfo GetTableInfo<TEntity>()
            where TEntity : class
        {
            var entityType = typeof(TEntity);
            lock (_tableInfos)
            {
                if (!_tableInfos.ContainsKey(entityType))
                {
                    CacheTableInfo(entityType);
                }

                return _tableInfos[entityType];
            }
        }

        private static void CacheInsertSql(TableInfo tableInfo)
        {
            IEnumerable<ColumnInfo> columsToInsert = tableInfo.ColumnInfos;
            if (tableInfo.AutoIncrement)
            {
                columsToInsert = tableInfo.ColumnInfos.Where(cm => cm.ColumnName != tableInfo.Key);
            }

            var fields = new List<string>();
            var values = new List<string>();
            foreach (var columnInfo in columsToInsert)
            {
                fields.Add($"[{columnInfo.ColumnName}]");
                values.Add("@" + columnInfo.FieldName);
            }

            var sql =
                $"INSERT INTO {tableInfo.TableName} ({string.Join(",", fields)}) \n" +
                (tableInfo.AutoIncrement ? $"OUTPUT inserted.{tableInfo.Key} \n" : "") +
                $"VALUES ({string.Join(",", values)});";

            tableInfo.SetInsertSql(sql);
        }

        private static void CacheUpdateSql(TableInfo tableInfo)
        {
            string predicate = null;
            var updates = new List<string>();
            foreach (var columnInfo in tableInfo.ColumnInfos)
            {
                if (columnInfo.ColumnName == tableInfo.Key)
                {
                    predicate = $"[{columnInfo.ColumnName}] = @{columnInfo.FieldName}";
                }
                else
                {
                    updates.Add($"[{columnInfo.ColumnName}] = @{columnInfo.FieldName}");
                }
            }

            var sql =
                $"UPDATE {tableInfo.TableName} SET\n" +
                $"\t{string.Join(",\n\t", updates)}\n" +
                $"WHERE {predicate};";

            tableInfo.SetUpdateSql(sql);
        }

        private void CacheTableInfo(Type entityType)
        {
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

            if (tableAttribute == null)
                throw new InvalidOperationException("Table attribute must be specified to the Entity");

            var tableInfo = new TableInfo(tableAttribute.Name);

            foreach (var propertyInfo in entityType.GetProperties())
            {
                var columnAttribute = propertyInfo.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    tableInfo.AddColumnMapping(columnAttribute.Name, propertyInfo.Name);
                    if (columnAttribute.IsPrimary)
                    {
                        tableInfo.SetKey(columnAttribute.Name);
                        tableInfo.SetAutoIncrement(columnAttribute.AutoIncrement);
                    }
                }
            }

            _tableInfos.Add(entityType, tableInfo);
        }
    }
}