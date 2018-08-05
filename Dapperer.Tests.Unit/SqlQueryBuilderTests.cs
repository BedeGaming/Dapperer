using System;
using Dapperer.QueryBuilders.MsSql;
using Xunit;

namespace Dapperer.Tests.Unit
{
    public class SqlQueryBuilderTests
    {
        [Fact]
        public void GetByPrimaryKeyQuery_EntityWithPrimaryKey_ReturnQueryAsExpected()
        {
            const string expectedSql = "SELECT * FROM TestTable WHERE Id = @Key";
            var queryBuilder = GetQueryBuilder();

            var sql = queryBuilder.GetByPrimaryKeyQuery<TestEntityWithAutoIncreamentId>();

            Assert.Equal(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Fact]
        public void GetByPrimaryKeyQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.GetByPrimaryKeyQuery<TestEntityNoTableSpecified>());

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void GetByPrimaryKeyQuery_EntityWithoutPrimaryKey_ThrowException()
        {
            const string expectedExceptionMessage = "Primary key must be specified to the table";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.GetByPrimaryKeyQuery<TestEntityWithoutPrimaryKey>());

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void PageQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.PageQuery<TestEntityNoTableSpecified>(2, 5));

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void PageQuery_NoOrderBySpecified_PrimaryKeyToOrderIsUsed()
        {
            const string expectedItemsSql = "SELECT * FROM TestTable ORDER BY Id OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(*) AS Int) AS total FROM TestTable";
            var queryBuilder = GetQueryBuilder();

            var pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncreamentId>(2, 5);

            Assert.Equal(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.Equal(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Fact]
        public void PageQuery_OrderBySpecified_PassedOrderByIsUsed()
        {
            const string expectedItemsSql = "SELECT * FROM TestTable ORDER BY Name OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(*) AS Int) AS total FROM TestTable";
            var queryBuilder = GetQueryBuilder();

            var pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncreamentId>(2, 5, orderByQuery: "ORDER BY Name");

            Assert.Equal(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.Equal(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Fact]
        public void PageQuery_FilterBySpecified_PassedFilterQueryIsUsed()
        {
            const string expectedItemsSql = "SELECT DISTINCT TestTable.* FROM TestTable WHERE BY Name like 'J%' ORDER BY Id OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(DISTINCT TestTable.Id) AS Int) AS total FROM TestTable WHERE BY Name like 'J%'";
            var queryBuilder = GetQueryBuilder();

            var pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncreamentId>(2, 5, filterQuery: "WHERE BY Name like 'J%'");

            Assert.Equal(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.Equal(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Fact]
        public void InsertQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.InsertQuery<TestEntityNoTableSpecified, int>());

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void InsertQuery_EntityWithAutoIncrementingPrimaryKey_DoesnotIncludePrimaryKeyInInsert()
        {
            const string expectedSql = "INSERT INTO TestTable ([Name]) OUTPUT inserted.Id VALUES (@Name);";
            var queryBuilder = GetQueryBuilder();

            var sql = queryBuilder.InsertQuery<TestEntityWithAutoIncreamentId, int>();

            Assert.Equal(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Fact]
        public void InsertQuery_EntityWithoutAutoIncrementingPrimaryKey_IncludePrimaryKeyInInsert()
        {
            const string expectedSql = "INSERT INTO TestTable ([Id],[Name]) VALUES (@Id,@Name);";
            var queryBuilder = GetQueryBuilder();

            var sql = queryBuilder.InsertQuery<TestEntityWithoutAutoIncreamentId, int>();

            Assert.Equal(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Fact]
        public void UpdateQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.UpdateQuery<TestEntityNoTableSpecified>());

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void UpdateQuery_EntityWithTableSpecified_ReturnExpectedInsertQuery()
        {
            const string expectedSql = "UPDATE TestTable SET[Name] = @NameWHERE [Id] = @Id;";
            var queryBuilder = GetQueryBuilder();

            var sql = queryBuilder.UpdateQuery<TestEntityWithAutoIncreamentId>();

            Assert.Equal(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Fact]
        public void DeleteQuery_EntityWithPrimaryKey_ReturnQueryAsExpected()
        {
            const string expectedSql = "DELETE FROM TestTable WHERE Id = @Key";
            var queryBuilder = GetQueryBuilder();

            var sql = queryBuilder.DeleteQuery<TestEntityWithAutoIncreamentId>();

            Assert.Equal(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Fact]
        public void DeleteQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.DeleteQuery<TestEntityNoTableSpecified>());

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        [Fact]
        public void DeleteQuery_EntityWithoutPrimaryKey_ThrowException()
        {
            const string expectedExceptionMessage = "Primary key must be specified to the table";
            var queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.DeleteQuery<TestEntityWithoutPrimaryKey>());

            Assert.Equal(expectedExceptionMessage, exception.Message);
        }

        private static string ReplaceNextLineAndTab(string sql)
        {
            return string.IsNullOrWhiteSpace(sql)
                ? sql
                : sql.Replace("\n", "").Replace("\t", "").Trim();
        }

        private IQueryBuilder GetQueryBuilder()
        {
            return new SqlQueryBuilder();
        }
    }
}
