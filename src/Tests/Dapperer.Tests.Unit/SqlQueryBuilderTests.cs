using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using Dapperer.QueryBuilders.MsSql;
using NUnit.Framework;

namespace Dapperer.Tests.Unit
{
    [TestFixture]
    public class SqlQueryBuilderTests
    {
        [Test]
        public void GetByPrimaryKeyQuery_EntityWithPrimaryKey_ReturnQueryAsExpected()
        {
            const string expectedSql = "SELECT * FROM TestTable WHERE Id = @Key";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            string sql = queryBuilder.GetByPrimaryKeyQuery<TestEntityWithAutoIncrementId>();

            Assert.AreEqual(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Test]
        public void GetByPrimaryKeyQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.GetByPrimaryKeyQuery<TestEntityNoTableSpecified>());

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void GetByPrimaryKeyQuery_EntityWithoutPrimaryKey_ThrowException()
        {
            const string expectedExceptionMessage = "Primary key must be specified to the table";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.GetByPrimaryKeyQuery<TestEntityWithoutPrimaryKey>());

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void GetByPrimaryKeyParameter_WithAnsiStringPrimaryKey_ReturnsAsExpected()
        {
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var parameter = queryBuilder.GetPrimaryKeyParameter<TestEntityWithAnsiStringPrimaryKey, string>("TestValue") as DbString;

            Assert.NotNull(parameter);
            Assert.IsTrue(parameter.IsAnsi);
            Assert.AreEqual("TestValue", parameter.Value);
        }

        [Test]
        public void GetByPrimaryKeyParameter_WithStringPrimaryKey_ReturnsAsExpected()
        {
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var parameter = queryBuilder.GetPrimaryKeyParameter<TestEntityWithStringPrimaryKey, string>("TestValue") as DbString;

            Assert.NotNull(parameter);
            Assert.IsFalse(parameter.IsAnsi);
            Assert.AreEqual("TestValue", parameter.Value);
        }

        [Test]
        public void GetByPrimaryKeyParameter_WithNonStringPrimaryKey_ReturnsAsExpected()
        {
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var parameter = queryBuilder.GetPrimaryKeyParameter<TestEntityWithAutoIncrementId, int>(51);
            Assert.AreEqual(51, (int)parameter);
        }

        [Test]
        public void GetByPrimaryKeyParameter_WithNNoPrimaryKey_ThrowsException()
        {
            const string expectedExceptionMessage = "Primary key must be specified to the table";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.GetPrimaryKeyParameter<TestEntityWithoutPrimaryKey, string>("TestValue"));

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void GetByPrimaryKeyParameters_WithAnsiStringPrimaryKey_ReturnsAsExpected()
        {
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var parameter = queryBuilder.GetPrimaryKeyParameters<TestEntityWithAnsiStringPrimaryKey, string>(new[] { "TestValue" }) as IEnumerable<DbString>;

            Assert.NotNull(parameter);
            Assert.IsTrue(parameter.First().IsAnsi);
            Assert.AreEqual("TestValue", parameter.First().Value);
        }

        [Test]
        public void GetByPrimaryKeyParameters_WithStringPrimaryKey_ReturnsAsExpected()
        {
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var parameter = queryBuilder.GetPrimaryKeyParameters<TestEntityWithStringPrimaryKey, string>(new[] { "TestValue" }) as IEnumerable<DbString>;

            Assert.NotNull(parameter);
            Assert.IsFalse(parameter.First().IsAnsi);
            Assert.AreEqual("TestValue", parameter.First().Value);
        }

        [Test]
        public void GetByPrimaryKeyParameters_WithNonStringPrimaryKey_ReturnsAsExpected()
        {
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var parameter = queryBuilder.GetPrimaryKeyParameters<TestEntityWithAutoIncrementId, int>(new[] { 51 }) as IEnumerable<int>;
            Assert.AreEqual(51, (int)parameter.First());
        }

        [Test]
        public void GetByPrimaryKeyParameters_WithNNoPrimaryKey_ThrowsException()
        {
            const string expectedExceptionMessage = "Primary key must be specified to the table";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Throws<InvalidOperationException>(() => queryBuilder.GetPrimaryKeyParameters<TestEntityWithoutPrimaryKey, string>(new[] { "TestValue" }));

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void PageQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.PageQuery<TestEntityNoTableSpecified>(2, 5));

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void PageQuery_NoOrderBySpecified_PrimaryKeyToOrderIsUsed()
        {
            const string expectedItemsSql = "SELECT * FROM TestTable ORDER BY Id OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(*) AS Int) AS total FROM TestTable";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            PagingSql pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncrementId>(2, 5);

            Assert.AreEqual(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.AreEqual(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Test]
        public void PageQuery_OrderBySpecified_PassedOrderByIsUsed()
        {
            const string expectedItemsSql = "SELECT * FROM TestTable ORDER BY Name OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(*) AS Int) AS total FROM TestTable";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            PagingSql pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncrementId>(2, 5, orderByQuery: "ORDER BY Name");

            Assert.AreEqual(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.AreEqual(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Test]
        public void PageQuery_FilterBySpecified_PassedFilterQueryIsUsed()
        {
            const string expectedItemsSql = "SELECT DISTINCT TestTable.* FROM TestTable WHERE BY Name like 'J%' ORDER BY Id OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(DISTINCT TestTable.Id) AS Int) AS total FROM TestTable WHERE BY Name like 'J%'";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            PagingSql pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncrementId>(2, 5, filterQuery: "WHERE BY Name like 'J%'");

            Assert.AreEqual(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.AreEqual(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Test]
        public void PageQuery_FilterBySpecified_And_AdditionalTableColumn_PassedQueryIsUsed()
        {
            const string expectedItemsSql = "SELECT DISTINCT Table, TableTest, TestTable.* FROM TestTable WHERE BY Name like 'J%' ORDER BY Id OFFSET 2 ROWS FETCH NEXT 5 ROWS ONLY";
            const string expectedCountSql = "SELECT CAST(COUNT(DISTINCT TestTable.Id) AS Int) AS total FROM TestTable WHERE BY Name like 'J%'";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            PagingSql pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncrementId>(2,
                                                                                         5,
                                                                                         filterQuery: "WHERE BY Name like 'J%'",
                                                                                         additionalTableColumns: new List<string> { "Table", "TableTest" });

            Assert.AreEqual(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.AreEqual(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Test]
        public void PageQuery_TakeIsZero_QueryForAllItemsIsReturned()
        {
            const string expectedItemsSql = "SELECT DISTINCT TestTable.* FROM TestTable WHERE BY Name like 'J%' ORDER BY Id OFFSET 2 ROWS";
            const string expectedCountSql = "SELECT CAST(COUNT(DISTINCT TestTable.Id) AS Int) AS total FROM TestTable WHERE BY Name like 'J%'";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            PagingSql pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncrementId>(2, 0, filterQuery: "WHERE BY Name like 'J%'");

            Assert.AreEqual(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.AreEqual(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Test]
        public void PageQuery_TakeIsZeroWithFilter_QueryForAllItemsIsReturned()
        {
            const string expectedItemsSql = "SELECT * FROM TestTable ORDER BY Id OFFSET 2 ROWS";
            const string expectedCountSql = "SELECT CAST(COUNT(*) AS Int) AS total FROM TestTable";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            PagingSql pagingSql = queryBuilder.PageQuery<TestEntityWithAutoIncrementId>(2, 0);

            Assert.AreEqual(expectedItemsSql, ReplaceNextLineAndTab(pagingSql.Items));
            Assert.AreEqual(expectedCountSql, ReplaceNextLineAndTab(pagingSql.Count));
        }

        [Test]
        public void InsertQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.InsertQuery<TestEntityNoTableSpecified, int>(multiple: false));

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void InsertQuery_EntityWithAutoIncrementingPrimaryKey_DoesnotIncludePrimaryKeyInInsert()
        {
            string expectedSql = new StringBuilder()
               .AppendLine("INSERT INTO TestTable")
                       .AppendLine("([Name])")
                       .AppendLine("VALUES")
                       .AppendLine(" (@Name);")
                       .AppendLine("")
                       .Append("SELECT CAST(SCOPE_IDENTITY() as Int);")
                   .ToString();

            IQueryBuilder queryBuilder = GetQueryBuilder();

            string sql = queryBuilder.InsertQuery<TestEntityWithAutoIncrementId, int>(multiple: false);

            Assert.AreEqual(expectedSql, sql);
        }

        [Test]
        public void InsertQuery_EntityWithAutoIncrementingPrimaryKeyAndIdentityInsert_IncludesIdentityInsert()
        {
            string expectedSql = new StringBuilder()
                .AppendLine("SET IDENTITY_INSERT TestTable ON")
                .AppendLine("INSERT INTO TestTable")
                    .AppendLine("([Id]")
                    .AppendLine(",[Name]")
                    .AppendLine(",[AdditionalField1]")
                    .AppendLine(",[AdditionalField2])")
                    .AppendLine("VALUES")
                    .AppendLine(" (@Id")
                    .AppendLine(",@Name")
                    .AppendLine(",@AdditionalField1")
                    .AppendLine(",@AdditionalField2);")
                .Append("SET IDENTITY_INSERT TestTable OFF")
                .ToString();

            IQueryBuilder queryBuilder = GetQueryBuilder();

            string sql = queryBuilder.InsertQuery<TestEntityWithExtraFields, int>(identityInsert: true);

            Assert.AreEqual(expectedSql, sql);
        }

        [Test]
        public void InsertQuery_EntityWithoutAutoIncrementingPrimaryKey_IncludePrimaryKeyInInsert()
        {
            string expectedSql = new StringBuilder()
                .AppendLine("INSERT INTO TestTable")
                    .AppendLine("([Id]")
                    .AppendLine(",[Name])")
                    .AppendLine("VALUES")
                    .AppendLine(" (@Id")
                    .Append(",@Name);")
                .ToString();
            IQueryBuilder queryBuilder = GetQueryBuilder();

            string sql = queryBuilder.InsertQuery<TestEntityWithoutAutoIncrementId, int>(multiple: false);

            Assert.AreEqual(expectedSql, sql);
        }

        [Test]
        public void UpdateQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.UpdateQuery<TestEntityNoTableSpecified>());

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void UpdateQuery_EntityWithTableSpecified_ReturnExpectedInsertQuery()
        {
            const string expectedSql = "UPDATE TestTable SET[Name] = @NameWHERE [Id] = @Id;";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            string sql = queryBuilder.UpdateQuery<TestEntityWithAutoIncrementId>();

            Assert.AreEqual(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Test]
        public void DeleteQuery_EntityWithPrimaryKey_ReturnQueryAsExpected()
        {
            const string expectedSql = "DELETE FROM TestTable WHERE Id = @Key";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            string sql = queryBuilder.DeleteQuery<TestEntityWithAutoIncrementId>();

            Assert.AreEqual(expectedSql, ReplaceNextLineAndTab(sql));
        }

        [Test]
        public void DeleteQuery_EntityWithoutTableSpecified_ThrowException()
        {
            const string expectedExceptionMessage = "Table attribute must be specified to the Entity";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.DeleteQuery<TestEntityNoTableSpecified>());

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        [Test]
        public void DeleteQuery_EntityWithoutPrimaryKey_ThrowException()
        {
            const string expectedExceptionMessage = "Primary key must be specified to the table";
            IQueryBuilder queryBuilder = GetQueryBuilder();

            var exception = Assert.Catch<InvalidOperationException>(() => queryBuilder.DeleteQuery<TestEntityWithoutPrimaryKey>());

            Assert.AreEqual(expectedExceptionMessage, exception.Message);
        }

        private string ReplaceNextLineAndTab(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            return sql.Replace("\n", "").Replace("\t", "").Trim();
        }

        private string ReplaceCarriageReturn(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
                return sql;

            return sql.Replace("\r", "");
        }

        private IQueryBuilder GetQueryBuilder()
        {
            return new SqlQueryBuilder();
        }
    }
}
