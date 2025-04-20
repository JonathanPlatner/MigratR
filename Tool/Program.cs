using System.CommandLine;
using Tool;

var rootCommand = new RootCommand("MigratR CLI tool for pure SQL database migrations");

var downCommand = new Command("down", "Rollback migration");
var downArgument = new Argument<int>(name: "count", description: "Number of migrations to roll back", getDefaultValue: () => 1);
downCommand.AddArgument(downArgument);
downCommand.SetHandler(count =>
{
    var runner = new MigrationRunner();
    runner.RollbackLast(count);
}, downArgument);

var newCommand = new Command("new", "Create a new migration file");
var newMigrationArgument = new Argument<string>(name: "name", description: "Name of the new migration file");
newCommand.AddArgument(newMigrationArgument);
newCommand.SetHandler(MigrationRunner.CreateNewMigration, newMigrationArgument);

var upCommand = new Command("up", "Apply all pending migrations");
upCommand.SetHandler(() =>
{
    var runner = new MigrationRunner();
    runner.MigrateUp();
});

var configCommand = new Command("init", "Create a new configuration file");
configCommand.SetHandler(ConfigHandler.Create);

rootCommand.AddCommand(configCommand);
rootCommand.AddCommand(upCommand);
rootCommand.AddCommand(downCommand);
rootCommand.AddCommand(newCommand);

await rootCommand.InvokeAsync(args);
