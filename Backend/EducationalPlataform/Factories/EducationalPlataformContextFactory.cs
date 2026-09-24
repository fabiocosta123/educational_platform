using EducationalPlataform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EducationalPlataform.Factories
{
    public class EducationalPlataformContextFactory : IDesignTimeDbContextFactory<EducationalPlataformContext>
    {
        public EducationalPlataformContext CreateDbContext(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var connectionString = NpgsqlConnectionString.Normalize(
                configuration.GetConnectionString("EducationalPlataformContext")
                    ?? throw new InvalidOperationException("Connection string 'EducationalPlataformContext' not found."));

            var optionsBuilder = new DbContextOptionsBuilder<EducationalPlataformContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new EducationalPlataformContext(optionsBuilder.Options);
        }
    }
}
