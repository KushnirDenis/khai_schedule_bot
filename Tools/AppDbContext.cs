using System.Reflection;
using khai_schedule_bot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace khai_schedule_bot.Tools;

public class AppDbContext : DbContext
{
    private readonly IConfiguration _configuration;
    public DbSet<Class> Classes { get; set; }
    public DbSet<Group> Groups { get; set; }
    public DbSet<BotUser> Users { get; set; }

    public AppDbContext()
    {
        _configuration = new ConfigurationBuilder()
            .AddUserSecrets(Assembly.GetExecutingAssembly(), true)
            .Build();
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public AppDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string? connectionString = _configuration.GetValue<string>("ConnectionString");
        
        if (connectionString is null)
        {
            Logger.Log("ConnectionString is null");
            throw new Exception("ConnectionString is null");
        }
        
        optionsBuilder.UseNpgsql(connectionString);
        
        
    }
}