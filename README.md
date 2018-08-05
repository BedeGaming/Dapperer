# Dapperer #

[![Build history](https://buildstats.info/appveyor/chart/josephjeganathan/Dapperer)](https://ci.appveyor.com/project/josephjeganathan/Dapperer/history)

[![Build status](https://ci.appveyor.com/api/projects/status/a2ibxbl95e3ogrgq?svg=true)](https://ci.appveyor.com/project/josephjeganathan/dapperer)

Dapperer is an extension for [Dapper](https://github.com/StackExchange/dapper-dot-net). It uses attributes on a database POCO entity classes to facilitate the followings.

- A generic repository for basic *CRUD* operations on a relational database with no SQL query.
- Populating sub entities
- Page-able queries
- Cache-able query builder for the basic CURD operation queries. 

# Nuget
https://www.nuget.org/packages/Dapperer

To install Dapperer, run the following command in the Package Manager Console
```
PM> Install-Package Dapperer
```

# Walk-through by example - Dapperer.TestApiApp

This is just a test application, ignore that the database entities are exposed to outside and error handling etc.

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

## Caching basic queries

In this current implementation of the **Dapperer** all the basic *CRUD* queries are cached in-memory. This need a single instance of a single Query builder for the lifetime of your applications. This can be wired using dependency injection. In the test application used in this Walk-through uses Autofac, and its binding as follow.

```C#
builder.RegisterType<SqlQueryBuilder>().As<IQueryBuilder>().SingleInstance();
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

