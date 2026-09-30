using EducationalPlataform.Data;
using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Services;

public sealed class DatabaseMigrationService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<DatabaseMigrationService> _logger;

    public DatabaseMigrationService(IServiceProvider services, ILogger<DatabaseMigrationService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EducationalPlataformContext>();
            try
            {
                await db.Database.MigrateAsync(stoppingToken);
                _logger.LogInformation("Database migrations applied.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database migration failed.");
            }

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "LateFeeApplied" boolean NOT NULL DEFAULT false;""",
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not ensure Payments.LateFeeApplied column.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database migration failed.");
        }
    }
}
