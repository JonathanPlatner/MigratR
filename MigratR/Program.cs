// See https://aka.ms/new-console-template for more information

using Microsoft.Extensions.Configuration;
using MigratR;

var configBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = configBuilder.Build();

var connectionString = configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("No connection string found.");
    return;
}

var runner = new MigrationRunner(connectionString);

if (args.Length == 0)
{
    runner.MigrateUp();
}
else if (args[0].Equals("rollback", StringComparison.CurrentCultureIgnoreCase))
{
    switch (args.Length)
    {
        case 1:
            // Roll back one migration by default.
            runner.RollbackLast(1);
            break;
        case 2 when int.TryParse(args[1], out int count):
            runner.RollbackLast(count);
            break;
        default:
            Console.WriteLine("Invalid rollback arguments. Usage: migrate rollback OR migrate rollback n");
            break;
    }
}
else if (args[0].Equals("new", StringComparison.CurrentCultureIgnoreCase))
{
    if (args.Length < 2)
    {
        Console.WriteLine("Please provide a migration name. Usage: migrate new 'name'");
    }
    else
    {
        var migrationName = args[1];
        runner.CreateNewMigration(migrationName);
    }
}
else
{
    Console.WriteLine("Unknown command. Available commands:");
    Console.WriteLine("  migrate                   // Apply all pending migrations");
    Console.WriteLine("  migrate rollback          // Rollback the last migration");
    Console.WriteLine("  migrate rollback n        // Rollback the last n migrations");
    Console.WriteLine("  migrate new 'name'        // Create a new migration file");
}
