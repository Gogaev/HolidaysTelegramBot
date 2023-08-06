using HolidaysTelegramBot.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace HolidaysTelegramBot
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<UserContext> Contexts { get; set; }
        
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }

        static ApplicationDbContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<States>();
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Gender>();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bot;Username=postgres;Password=password;Integrated Security=false;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresEnum<States>();
            modelBuilder.HasPostgresEnum<Gender>();
        }
    }
}
