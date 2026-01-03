using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Core.Persistence.Context;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var rootDir = FindRootDirectory(currentDir);
        var webProjectPath = Path.Combine(rootDir, "src", "Web");

        LoadModuleAssemblies(rootDir);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(webProjectPath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3);
            npgsqlOptions.MigrationsAssembly("Core.Persistence");
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static void LoadModuleAssemblies(string rootDir)
    {
        var modulesPath = Path.Combine(rootDir, "src", "Modules");
        
        if (!Directory.Exists(modulesPath))
            return;

        var persistenceAssemblies = Directory.GetFiles(modulesPath, "*.Persistence.dll", SearchOption.AllDirectories);
        
        foreach (var assemblyPath in persistenceAssemblies)
        {
            try
            {
                Assembly.LoadFrom(assemblyPath);
            }
            catch
            {
            }
        }
    }

    private static string FindRootDirectory(string currentDirectory)
    {
        var directory = new DirectoryInfo(currentDirectory);
        
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src")) ||
                Directory.GetFiles(directory.FullName, "*.sln").Length > 0)
            {
                return directory.FullName;
            }
            
            directory = directory.Parent;
        }
        
        throw new InvalidOperationException(
            "Could not find root directory. Make sure you're running from within the project structure.");
    }
}