using System.Reflection;
using khai_schedule_bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer.Migrations.Internal;
using Microsoft.Extensions.Configuration;

namespace khai_schedule_bot.Tools;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    public int TotalFaculties { get; } = 8;
    public DbSet<Class> Classes { get; set; }
    public DbSet<Group> Groups { get; set; }

    public AppDbContext()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .Build();
    }

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string? connectionString = _configuration.GetValue<string>("ConnectionString");
        
        if (connectionString is null)
        {
            Logger.Log("ConnectionString is null");
            throw new Exception("ConnectionString is null");
        }
        
        optionsBuilder.UseSqlServer(connectionString);
    }
}