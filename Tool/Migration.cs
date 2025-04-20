using Microsoft.Data.SqlClient;

namespace Tool;

public record Migration
{
    public required string FilePath { get; init; }
    public string FileName => Path.GetFileName(FilePath);
    public required string UpScript { get; init; }
    public required string DownScript { get; init; }
}

public class MigrationRunner
{
    private readonly string _connectionString;
    public string MigrationsFolder { get; }
    public const string Delimeter = "--//@ ```MIGRATION SEPARATOR: DO NOT DELETE THIS LINE```";

    public MigrationRunner()
    {
        _connectionString = ConfigHandler.GetConnectionString();
        MigrationsFolder = ConfigHandler.GetMigrationsFolder();

        if (!Directory.Exists(MigrationsFolder))
        {
            Directory.CreateDirectory(MigrationsFolder);
        }
    }

    public void EnsureHistoryTable()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        const string commandText = """
                                   if not exists(select * from sys.tables where name = 'migrations')
                                   begin                
                                   create table migrations (
                                       id int identity primary key,
                                       migration_file nvarchar(255) not null,
                                       applied_on datetime2
                                   );
                                   end
                                   """;
        using var command = new SqlCommand(commandText, connection);
        command.ExecuteNonQuery();
    }

    public List<string> GetAppliedMigrations()
    {
        const string commandText = "select migration_file from migrations order by applied_on";
        var appliedMigrations = new List<string>();
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(commandText, connection);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            appliedMigrations.Add(reader.GetString(0));
        }
        return appliedMigrations;
    }

    public Migration ParseMigrationFile(string filePath)
    {
        var content = File.ReadAllText(filePath);

        var parts = content.Split(Delimeter);
        if (parts.Length != 2)
        {
            throw new FormatException("Invalid migration file");
        }

        return new Migration
        {
            FilePath = filePath,
            UpScript = parts[0],
            DownScript = parts[1],
        };
    }

    public void ApplyMigration(Migration migration)
    {
        Console.WriteLine($"Applying {migration.FileName}");
        const string historySql = "insert into migrations (migration_file, applied_on) values (@file, @appliedOn)";
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        using var migrateCommand = new SqlCommand(migration.UpScript, connection);
        migrateCommand.ExecuteNonQuery();

        using var command = new SqlCommand(historySql, connection);
        command.Parameters.AddWithValue("@file", Path.GetFileName(migration.FilePath));
        command.Parameters.AddWithValue("@appliedOn", DateTime.Now);
        command.ExecuteNonQuery();
    }

    public void RollbackMigration(Migration migration)
    {
        Console.WriteLine($"Reverting {migration.FileName}");
        const string historySql = "delete from migrations where migration_file = @file";
        using var connection = new SqlConnection(_connectionString);
        connection.Open();

        var rollbackCommand = new SqlCommand(migration.DownScript, connection);
        rollbackCommand.ExecuteNonQuery();

        using var historyCommand = new SqlCommand(historySql, connection);
        historyCommand.Parameters.AddWithValue("@file", Path.GetFileName(migration.FilePath));
        historyCommand.ExecuteNonQuery();
    }

    public void MigrateUp()
    {
        EnsureHistoryTable();
        var files = Directory.GetFiles(MigrationsFolder, "*.sql").OrderBy(f => f).ToList();
        var appliedMigrations = GetAppliedMigrations();
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            if (appliedMigrations.Contains(fileName))
            {
                Console.WriteLine($"Skipping {fileName}");
                continue;
            }

            var migration = ParseMigrationFile(file);
            ApplyMigration(migration);
        }
    }

    public void RollbackLast(int count = 1)
    {
        EnsureHistoryTable();
        var appliedMigrations = GetAppliedMigrations();
        if (appliedMigrations.Count == 0)
        {
            Console.WriteLine("No migrations have been applied");
            return;
        }

        var migrationsToRollback = appliedMigrations.Reverse<string>().Take(count).ToList();
        foreach (var migrationFile in migrationsToRollback)
        {
            var filePath = Path.Combine(MigrationsFolder, migrationFile);
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Migration file {migrationFile} not found. Aborting.");
                return;
            }

            var migration = ParseMigrationFile(filePath);
            RollbackMigration(migration);
        }
    }

    public static void CreateNewMigration(string migrationFileName)
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        var escapedName = migrationFileName.Replace(" ", "_");
        var migrationsFolder = ConfigHandler.GetMigrationsFolder();
        var directory = Directory.GetCurrentDirectory();
        if (!Directory.Exists(Path.Combine(directory, migrationsFolder)))
        {
            Directory.CreateDirectory(Path.Combine(directory, migrationsFolder));
        }
        var fileName = $"{timestamp}_{escapedName}.sql";

        var filePath = Path.Combine(migrationsFolder, fileName);

        var content = $"""
                       -- Migration: {fileName}

                       -- UP
                       -- Write your forward migration SQL statements here

                       {Delimeter}

                       -- DOWN
                       -- Write your rollback migration SQL statements here
                       """;

        File.WriteAllText(filePath, content);
        Console.WriteLine($"Created new migration file: {filePath}");
    }
}