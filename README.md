
[![Build status](https://dev.azure.com/windylulu/EntityFrameworkCore.DBCopy/_apis/build/status/EntityFrameworkCore.DBCopy-ASP.NET%20Core-CI)](https://dev.azure.com/windylulu/EntityFrameworkCore.DBCopy/_build/latest?definitionId=1)
[![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/EntityFrameworkCore.DBCopy?label=EntityFrameworkCore.DBCopy)](https://www.nuget.org/packages/EntityFrameworkCore.DBCopy)
# EntityFrameworkCore.DBCopy
Copy database using EFcore
This is a project for transferring data between different databases,Even databases of different structures.
## Features
Support SQL Server 2008+,SQL Azure,SQL Compact,Oracle,MySQL,PostgreSQL,SQLite..
Staging data to continue importing exports..
Check and list the ShadowPropery.
## Before use this

1.Prepare the migration files for the destination database,An easy way is to inherit your Dbcontext.

```C#
 public class AAContext : DbContext
 {
    //you use this Context,like sql server
 }
 
  public class MysqlAAContext : AAContext
 {
    //this Context is using to Mysql
 }
 
  public class PostgresqlAAContext : AAContext
 {
    //this Context is using to Postgresql
 }
 ```
 You still use AAContext to crud,other Context only use to migrations.
 
 You can map the column type you like
 ```C#
 public class AAContext : DbContext
 {
    //you use this Context
     protected override void OnModelCreating(ModelBuilder modelBuilder)
     {
        if (Database.IsNpgsql())
        {
          modelBuilder.HasPostgresExtension("citext");
          modelBuilder.Entity<xxx>().Property(d => d.xxx).HasColumnType("citext");
        }
        if (Database.IsMySql())
        {
         xxxx
        }
     }
 }
 ```
 2.Update your IDesignTimeDbContextFactory <br>
 ```C#
 ///Default factory
   public class AAContextContextFactory : IDesignTimeDbContextFactory<AAContext>
    {
     
        public AAContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<AAContext>();
            builder.UseSqlServer(connectionString);

            return new AAContext(builder.Options);
        }
    }
    ///New factory
   public class MysqlAAContextContextFactory : IDesignTimeDbContextFactory<MysqlAAContext>
    {
     
        public MysqlAAContext CreateDbContext(string[] args)
        {
            var builder = new DbContextOptionsBuilder<MysqlAAContext>();
            builder.UseMySql(connectionString);

            return new MysqlAAContext(builder.Options);
        }
    }
 ```
 
Then call
 ```
    dotnet ef migrations add xxx --context MysqlAAContext --output-dir Migrations/MySqlMigrations --project xxxEntityFrameworkCore --startup-project xxx
    dotnet ef database update  --context MysqlAAContext --project xxxEntityFrameworkCore --startup-project xxx
 ```
 3.Fix Shadow Properties
 See https://docs.microsoft.com/en-us/ef/core/modeling/shadow-properties
 You must explicitly declare the shadow property,if you don't, the shadow-properties's data will lose.
 Fortunately, we check when initialized.You can set IgnoreShadowPropery=true to igonre the error.
## Use this project
 ```C#
   var fromoptionsBuilder = new DbContextOptionsBuilder<AAContext>();
       fromoptionsBuilder.UseSqlServer(xxxx);
   var tooptionsBuilder = new DbContextOptionsBuilder<AAContext>();
       tooptionsBuilder.UseMySql(xxxx);
   var fromdboption = fromoptionsBuilder.Options;
   var todboption = tooptionsBuilder.Options;
   var copy = new DBCopyWorker<SkuContext>(fromdboption, todboption);
       copy.Copy();
```
## Attention
This repository use [Entity Framework Extensions](https://entityframework-extensions.net/?z=github&y=entityframework-plus)
to bulkinsert temporary.It is not free,but you can get free trial.I will replace this in the funture!
