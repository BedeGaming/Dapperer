# Dapperer #

[![Build history](https://buildstats.info/appveyor/chart/josephjeganathan/Dapperer)](https://ci.appveyor.com/project/josephjeganathan/Dapperer/history)

[![Build status](https://ci.appveyor.com/api/projects/status/a2ibxbl95e3ogrgq?svg=true)](https://ci.appveyor.com/project/josephjeganathan/Dapperer) [![](http://img.shields.io/nuget/v/Dapperer.svg?style=flat-square)](http://www.nuget.org/packages/Dapperer/)  [![](http://img.shields.io/nuget/dt/Dapperer.svg?style=flat-square)](http://www.nuget.org/packages/Dapperer/)

Dapperer is an extension for [Dapper](https://github.com/StackExchange/dapper-dot-net). It uses attributes on a database POCO entity classes to facilitate the followings.

- A generic repository for basic *CRUD* operations on a relational database with no SQL query.
- Populating sub entities
- Page-able queries
- Cache-able query builder for the basic CURD operation queries. 

# Install
https://www.nuget.org/packages/Dapperer

To install Dapperer, run the following command in the Package Manager Console
```
PM> Install-Package Dapperer
```

# Walk-through by example

Ref: *Dapperer.TestApiApp*, please ignore the API best practices.

## Database entities and Attributes 

```C#
[Table("Contacts")]
public class Contact : IIdentifier<int>
{
    [Column("Id", IsPrimary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column("Name")]
    public string Name { get; set; }

    public List<Address> Addresses { get; set; }

    public void SetIdentity(int identity)
    {
        Id = identity;
    }

    public int GetIdentity()
    {
        return Id;
    }
}

```
A database entity class must be extended from `IIdentifier<TPrimaryKey>`
- `Table` attribute is to specify table name
- `Column` attribute is to specify column name and  more such as  
  - `IsPrimary` - Primary key or not
  - `AutoIncrement` if not auto increment then the key must be set before add/update an entity

 
## Repositories 

```C#
public class ContactRepository : Repository<Contact, int>
{
    public ContactRepository(IQueryBuilder queryBuilder, IDbFactory dbFactory)
        : base(queryBuilder, dbFactory)
    {
    }

    public virtual void PopulateAddresses(Contact contact)
    {
        PopulateOneToMany<Address, int>(address => address.ContactId, c => c.Addresses, contact);
    }

    public virtual Contact GetContactByName(string name)
    {
        ITableInfoBase tableInfo = GetTableInfo();
        string sql = string.Format(@"SELECT * FROM {0} WHERE Name = @Name", tableInfo.TableName);

        using (IDbConnection connection = CreateConnection())
        {
            return connection.Query<Contact>(sql, new { Name = name }).SingleOrDefault();
        }
    }
}
```

**Dapperer** conversion for repository is one concrete repository per database entity. A concrete repository (`ContactRepository` in the above case) must be extended from the base `Repository<TEntity, TPrimaryKey>` which provides all the basic *CRUD* operations, page-able queries, etc. You can enjoy everything dapper provides in the extended repository class, `GetContactByName` is an example for writing your own custom queries.

### What if you don't want the basic CRUD functionality?

You can always use the core Dapper, using `IDbFactory`.

```C#
public class UserRepository : IUserRepository
{
    private readonly IDbFactory _dbFactory;

    public UserRepository(IDbFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        var sql = "SELECT * FROM Users WHERE email = @Email";

        using (var connection = _dbFactory.CreateConnection())
        {
            return (await connection.QueryAsync<User>(sql, new
            {
                Email = email
            }).SingleOrDefault();
        }
    }
}

```
 

## Caching basic queries

In this current implementation of the **Dapperer** all the basic *CRUD* queries are cached in-memory. This need a single instance of a single Query builder for the lifetime of your applications. This can be wired using dependency injection. In the test application used in this Walk-through uses Autofac, and its binding as follow.

```C#
builder.RegisterType<SqlQueryBuilder>().As<IQueryBuilder>().SingleInstance();
```

## MS SQL Extras

**[Table-Valued Parameters]** are strongly typed user defined type that can be used in MS SQL database queries. [IntList], [LogList], [StringList]  and [GuidList] are included part as helpers. 

#### How to use them?
It's important to note that the custom types must exist in the database, check out the [TVP sql here][TVP SQL]. For example,

```SQL
CREATE TYPE IntList AS TABLE (
	Id INT NOT NULL PRIMARY KEY
);
```

```C#
public async Task<IList<User>> GetUsersAsync(IList<int> userIds)
{
    using (var connection = _dbFactory.CreateConnection())
    {
        const string sql = @"
            SELECT u.* 
            FROM @UserIds uid
            INNER JOIN Users u ON uid.Id = u.Id";

        return (await connection.QueryAsync<User>(sql, new
        {
            UserIds = new IntList(userIds).AsTableValuedParameter()
        })).ToList();
    }
}
```

## Custom column mapping

It is **important** to note that if the if the column name in the database does not match the POCO entity class' property name then the POCO entity will not populate the right database value instead it'll be use default value of the property type.

Luckily Dapper solves that issues with custom column mappings, we leverage that to support our POCO entities with `Table` and `Column` attributes.

**Registering Dapperer Mapping**

For example, all the database entity class are in the same assembly as `User` database entity.

```C#
[Table("User")]
public class User : IIdentifier<int>
{
    [Column("UserId", IsPrimary = true, AutoIncrement = true)]
    public int Id { get; set; }

    [Column("User_Name")]
    public string Name { get; set; }

    public void SetIdentity(int identity) => Id = identity;

    public int GetIdentity() => Id;
}
``` 

**Register column mappings** - this should be done once, and can be done during the application initialization.
```C#
typeof(User).Assembly.UseDappererColumnMapping();
```

## Configurations 

**Dapperer** need a concrete implementation of `IDappererSettings`, take a look at `DefaultDappererSettings` in the example project. 

```XML
<add key="Dapperer.ConnectionString" value="Server=localhost;Database=dapper_test;Trusted_Connection=True;" />
```

## Databases

**Dapperer** can be extended for different databases other than *SQL* database you must create the following for any new databases
- QueryBuilder for the database which extends `IQueryBuilder`
- DbFactory for creating database connection which extends `IDbFactory`

# Contributing to Dapperer

Please refer [CONTRIBUTING](CONTRIBUTING)


[IntList]: ./Dapperer/QueryBuilders/MsSql/TableValueParams/IntList.cs
[LogList]: ./Dapperer/QueryBuilders/MsSql/TableValueParams/LongList.cs
[StringList]: ./Dapperer/QueryBuilders/MsSql/TableValueParams/StringList.cs
[GuidList]: ./Dapperer/QueryBuilders/MsSql/TableValueParams/GuidList.cs
[TVP SQL]: ./db/TVP.sql
[Table-Valued Parameters]: https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/table-valued-parameters
