# EntityFrameworkCore.DBCopy
Copy database using EFcore
This is a library for transferring data between different databases,Even databases of different structures.
## Before use this

Prepare the migration files for the destination database,An easy way is to inherit your Dbcontext.

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
 Update your IDesignTimeDbContextFactory <br>
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
## Use this library
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
## Limit
This repository use 
---

[Entity Framework Extensions](https://entityframework-extensions.net/?z=github&y=entityframework-plus)

<a href="https://entityframework-extensions.net/?z=github&y=entityframework-plus">
<kbd>
<img src="https://zzzprojects.github.io/images/logo/entityframework-extensions-pub.jpg" alt="Entity Framework Extensions" />
</kbd>
</a>

---
to bulkinsert.
It is not free,but you can get free trial.I will replace this in the funture!
