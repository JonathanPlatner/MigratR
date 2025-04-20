using System.Text.Json;

namespace Tool;
public record Config(string ConnectionString, string MigrationsFolder = "Migrations");
public static class ConfigHandler
{
    public static void Create()
    {
        var directory = Directory.GetCurrentDirectory();
        var migratRDir = Path.Combine(directory, ".MigratR");

        if (!Directory.Exists(migratRDir))
        {
            Directory.CreateDirectory(migratRDir);
            Console.WriteLine($"Created directory: {migratRDir}");
        }

        var configFilePath = Path.Combine(migratRDir, "config.json");

        var defaultConfig = new Config(ConnectionString: "");

        var configJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(configFilePath, configJson);
    }

    public static string GetConnectionString()
    {

        var config = GetConfig();
        if (string.IsNullOrEmpty(config.ConnectionString))
        {
            throw new InvalidOperationException("Connection string is missing or empty in the configuration file");
        }

        return config.ConnectionString;
    }

    public static string GetMigrationsFolder()
    {
        var config = GetConfig();
        if (string.IsNullOrEmpty(config.MigrationsFolder))
        {
            throw new InvalidOperationException("Migrations folder is missing or empty in the configuration file");
        }
        return config.MigrationsFolder;
    }

    static Config GetConfig()
    {
        var directory = Directory.GetCurrentDirectory();
        var configFilePath = Path.Combine(directory, ".MigratR", "config.json");

        if (!File.Exists(configFilePath))
        {
            throw new FileNotFoundException($"Configuration file not found at: {configFilePath}. Create one with `MigratR init`");
        }

        var json = File.ReadAllText(configFilePath);
        var config = JsonSerializer.Deserialize<Config>(json);
        if (config == null)
        {
            throw new InvalidCastException("Unable to deserialize config object");
        }
        return config;
    }
}