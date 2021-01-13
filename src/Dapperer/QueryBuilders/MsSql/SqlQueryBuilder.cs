using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Dapper;

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
            TableInfo tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            return string.Format("SELECT * FROM {0} WHERE {1} = @Key", tableInfo.TableName, tableInfo.Key);
        }

        public string GetByPrimaryKeysQuery<TEntity>()
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            return string.Format("SELECT * FROM {0} WHERE {1} IN @Keys", tableInfo.TableName, tableInfo.Key);
        }

        public object GetPrimaryKeyParameter<TEntity, TPrimaryKey>(TPrimaryKey key)
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            if (key is string stringKey)
            {
                return new DbString
                {
                    IsAnsi = tableInfo.KeyIsAnsi,
                    Value = stringKey
                };
            }

            return key;
        }

        public object GetPrimaryKeyParameters<TEntity, TPrimaryKey>(IEnumerable<TPrimaryKey> keys)
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            if (keys is IEnumerable<string> stringKeys)
            {
                return keys.Select(key => new DbString
                {
                    IsAnsi = tableInfo.KeyIsAnsi,
                    Value = key as string
                });
            }

            return keys;
        }

        public string GetAll<TEntity>()
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();
            return string.Format("SELECT * FROM {0} ", tableInfo.TableName);
        }

        public PagingSql PageQuery<TEntity>(long skip, long take, string orderByQuery = null, string filterQuery = null, ICollection<string> additionalTableColumns = null)
            where TEntity : class
        {
            ITableInfoBase tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(orderByQuery))
            {
                orderByQuery = string.Format("ORDER BY {0}", tableInfo.Key);
            }

            var selectColumns = GetSelectColumns(additionalTableColumns, tableInfo);

            var pagingSql = new PagingSql();
            if (string.IsNullOrWhiteSpace(filterQuery))
            {
                if (take == 0)
                {
                    pagingSql.Items = string.Format("SELECT * FROM {0} {1} OFFSET {2} ROWS", tableInfo.TableName, orderByQuery, skip);
                }
                else
                {
                    pagingSql.Items = string.Format("SELECT * FROM {0} {1} OFFSET {2} ROWS FETCH NEXT {3} ROWS ONLY", tableInfo.TableName, orderByQuery, skip, take);
                }
                pagingSql.Count = string.Format("SELECT CAST(COUNT(*) AS Int) AS total FROM {0}", tableInfo.TableName);
            }
            else
            {
                if (take == 0)
                {
                    pagingSql.Items = string.Format("SELECT DISTINCT {0} FROM {1} {2} {3} OFFSET {4} ROWS", 
                                                    selectColumns, 
                                                    tableInfo.TableName, 
                                                    filterQuery, 
                                                    orderByQuery, 
                                                    skip);
                }
                else
                {
                    pagingSql.Items = string.Format("SELECT DISTINCT {0} FROM {1} {2} {3} OFFSET {4} ROWS FETCH NEXT {5} ROWS ONLY", 
                                                    selectColumns, 
                                                    tableInfo.TableName, 
                                                    filterQuery, 
                                                    orderByQuery, 
                                                    skip,
                                                    take);
                }
                pagingSql.Count = string.Format("SELECT CAST(COUNT(DISTINCT {0}.{1}) AS Int) AS total FROM {0} {2}", tableInfo.TableName, tableInfo.Key, filterQuery);
            }

            return pagingSql;
        }

        private string GetSelectColumns(ICollection<string> additionalTableColumns, ITableInfoBase tableInfo)
        {
            if (additionalTableColumns == null)
            {
                additionalTableColumns = new List<string>();
            }

            additionalTableColumns.Add($"{tableInfo.TableName}.*");
            return string.Join(", ", additionalTableColumns);
        }

        public string InsertQuery<TEntity, TPrimaryKey>(bool multiple = false)
            where TEntity : class
        {
            return InsertQuery<TEntity, TPrimaryKey>(multiple: multiple, identityInsert: false);
        }

        public string InsertQuery<TEntity, TPrimaryKey>(bool multiple = false, bool identityInsert = false)
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();

            if (identityInsert)
            {
                return IdentityInsertQuery(tableInfo);
            }

            lock (tableInfo)
            {
                if (string.IsNullOrWhiteSpace(tableInfo.InsertSql))
                {
                    CacheInsertSql(tableInfo);
                }
            }

            if (multiple || !tableInfo.AutoIncrement)
            {
                return tableInfo.InsertSql;
            }

            return $"{tableInfo.InsertSql}{Environment.NewLine}{LastInsertedIdQuery<TPrimaryKey>()}";
        }

        public string InsertQueryBatch<TEntity>(
            IEnumerable<TEntity> entities,
            string tableName,
            string[] columnNames,
            bool identityInsert = false)
             where TEntity : class
        {
            var sql = ToSqlInsertStatement(entities, tableName, columnNames);

            if (identityInsert)
            {
                string identityInsertOn = $"SET IDENTITY_INSERT {tableName} ON";
                string identityInsertOff = $"SET IDENTITY_INSERT {tableName} OFF";

                return $"{identityInsertOn}{Environment.NewLine}{sql}{Environment.NewLine}{identityInsertOff}";
            }

            return sql;
        }

        private static string IdentityInsertQuery(TableInfo tableInfo)
        {
            lock (tableInfo)
            {
                if (string.IsNullOrWhiteSpace(tableInfo.IdentityInsertSql))
                {
                    CacheIdentityInsertSql(tableInfo);
                }
            }

            string identityInsertOn = $"SET IDENTITY_INSERT {tableInfo.TableName} ON";
            string identityInsertOff = $"SET IDENTITY_INSERT {tableInfo.TableName} OFF";

            return $"{identityInsertOn}{Environment.NewLine}{tableInfo.IdentityInsertSql}{Environment.NewLine}{identityInsertOff}";
        }

        public string UpdateQuery<TEntity>()
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();

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
            TableInfo tableInfo = GetTableInfo<TEntity>();

            if (string.IsNullOrWhiteSpace(tableInfo.Key))
                throw new InvalidOperationException("Primary key must be specified to the table");

            return DeleteQuery<TEntity>(string.Format("WHERE {0} = @Key", tableInfo.Key));
        }

        public string DeleteQuery<TEntity>(string filterQuery)
            where TEntity : class
        {
            TableInfo tableInfo = GetTableInfo<TEntity>();

            return string.Format("DELETE FROM {0} {1}", tableInfo.TableName, filterQuery);
        }

        public ITableInfoBase GetBaseTableInfo<TEntity>()
            where TEntity : class
        {
            return GetTableInfo<TEntity>();
        }

        private TableInfo GetTableInfo<TEntity>()
            where TEntity : class
        {
            Type entityType = typeof(TEntity);
            lock (_tableInfos)
            {
                if (!_tableInfos.ContainsKey(entityType))
                {
                    CacheTableInfo(entityType);
                }
            }

            return _tableInfos[entityType];
        }

        private static void CacheInsertSql(TableInfo tableInfo)
        {
            IEnumerable<ColumnInfo> columsToInsert = tableInfo.ColumnInfos;
            if (tableInfo.AutoIncrement)
            {
                columsToInsert = tableInfo.ColumnInfos.Where(cm => cm.ColumnName != tableInfo.Key);
            }

            string sql = GenerateInsertSql(tableInfo, columsToInsert);

            tableInfo.SetInsertSql(sql);
        }

        private static void CacheIdentityInsertSql(TableInfo tableInfo)
        {
            string sql = GenerateInsertSql(tableInfo, tableInfo.ColumnInfos);

            tableInfo.SetIdentityInsertSql(sql);
        }

        private static string GenerateInsertSql(TableInfo tableInfo, IEnumerable<ColumnInfo> columsToInsert)
        {
            var fields = new List<string>();
            var values = new List<string>();
            foreach (ColumnInfo columnInfo in columsToInsert)
            {
                fields.Add(string.Format("[{0}]", columnInfo.ColumnName));
                values.Add("@" + columnInfo.FieldName);
            }

            string fieldsJoined = string.Join($"{Environment.NewLine},", fields);
            string valuesJoined = string.Join($"{Environment.NewLine},", values);

            string sql = $"INSERT INTO {tableInfo.TableName}{Environment.NewLine}" +
                $"({fieldsJoined}){Environment.NewLine}" +
                $"VALUES{Environment.NewLine} ({valuesJoined});";
            return sql;
        }

        private static string LastInsertedIdQuery<TPrimaryKey>()
        {
            string getIdQuery;
            if (typeof(TPrimaryKey) == typeof(int))
            {
                getIdQuery = "SELECT CAST(SCOPE_IDENTITY() as Int)";
            }
            else
            {
                getIdQuery = "SELECT CAST(SCOPE_IDENTITY() as BigInt)";
            }
            return $"{Environment.NewLine}{getIdQuery};";
        }

        private static void CacheUpdateSql(TableInfo tableInfo)
        {
            string predicate = null;
            var updates = new List<string>();
            foreach (ColumnInfo columnInfo in tableInfo.ColumnInfos)
            {
                if (columnInfo.ColumnName == tableInfo.Key)
                {
                    predicate = string.Format("[{0}] = @{1}", columnInfo.ColumnName, columnInfo.FieldName);
                }
                else
                {
                    updates.Add(string.Format("[{0}] = @{1}", columnInfo.ColumnName, columnInfo.FieldName));
                }
            }

            string sql = string.Format(
                "UPDATE {0} SET\n" +
                "\t{1}\n" +
                "WHERE {2};",
                tableInfo.TableName, string.Join(",\n\t", updates), predicate);

            tableInfo.SetUpdateSql(sql);
        }

        private void CacheTableInfo(Type entityType)
        {
            var tableAttribute = entityType.GetCustomAttribute<TableAttribute>();

            if (tableAttribute == null)
                throw new InvalidOperationException("Table attribute must be specified to the Entity");

            var tableInfo = new TableInfo(tableAttribute.Name);

            foreach (PropertyInfo propertyInfo in entityType.GetProperties())
            {
                var columnAttribute = propertyInfo.GetCustomAttribute<ColumnAttribute>();
                if (columnAttribute != null)
                {
                    tableInfo.AddColumnMapping(columnAttribute.Name, propertyInfo.Name);
                    if (columnAttribute.IsPrimary)
                    {
                        tableInfo.SetKey(columnAttribute.Name);
                        tableInfo.SetKeyAnsi(columnAttribute.IsAnsi);
                        tableInfo.SetAutoIncrement(columnAttribute.AutoIncrement);
                    }
                }
            }

            _tableInfos.Add(entityType, tableInfo);
        }

        private string ToSqlInsertStatement<TEntity>(IEnumerable<TEntity> data, string tableName, string[] columnNames)
            where TEntity : class
        {
            return ConvertToSqlInsertStatement(data, tableName, columnNames);
        }

        private string ConvertToSqlInsertStatement<T>(IEnumerable<T> data, string tableName, IEnumerable<string> columns)
        {
            StringBuilder sb = new StringBuilder();

            string columnsJoined = string.Join($",", columns.Select(x => $"[{x}]"));

            sb.Append($"INSERT INTO {tableName} ({columnsJoined}) VALUES ");

            sb.Append(Environment.NewLine);
            for (int i = 0; i < data.Count(); i++)
            {
                string parameters = string.Join($",", columns.Select(x => $"@{x}{i}"));
                sb.Append($"({parameters}),");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }
    }
}