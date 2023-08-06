using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HolidaysTelegramBot;

public class SampleContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var builder = new ConfigurationBuilder();
        
        builder.AddEnvironmentVariables();
        IConfiguration configuration = builder.Build();
        
        var connectionString = configuration["DbConnectionString"];
        
        optionsBuilder.UseNpgsql(connectionString, opts => opts.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds));
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}