using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Data;

public static class SchemaEnsure
{
    public static readonly string[] RequiredPaymentColumns =
    {
        "SettledAt",
        "InstallmentNumber",
        "LateFeeApplied",
        "PixInvoiceId",
        "PixTransactionId",
        "PixCopyPaste",
        "PixExpiresAt"
    };

    public static readonly string[] RequiredCourseColumns =
    {
        "Price",
        "InstallmentCount"
    };

    private static readonly string[] Statements =
    {
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "SettledAt" timestamp without time zone NULL;""",
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "InstallmentNumber" integer NULL;""",
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "LateFeeApplied" boolean NOT NULL DEFAULT false;""",
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixInvoiceId" text NULL;""",
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixTransactionId" text NULL;""",
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixCopyPaste" text NULL;""",
        """ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixExpiresAt" timestamp without time zone NULL;""",
        """ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Price" numeric(10,2) NOT NULL DEFAULT 0;""",
        """ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "InstallmentCount" integer NOT NULL DEFAULT 12;"""
    };

    public static DateTimeOffset? LastAppliedAt { get; private set; }
    public static string? LastError { get; private set; }

    public static void Apply(EducationalPlataformContext db)
    {
        LastError = null;
        var errors = new List<string>();
        foreach (var sql in Statements)
        {
            try
            {
                db.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                errors.Add($"{sql} → {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        LastError = errors.Count == 0 ? null : string.Join(" | ", errors);
        if (errors.Count == 0)
            LastAppliedAt = DateTimeOffset.UtcNow;
    }

    public static Task ApplyAsync(EducationalPlataformContext db, CancellationToken cancellationToken = default)
    {
        Apply(db);
        return Task.CompletedTask;
    }

    public static async Task<object> DescribeAsync(EducationalPlataformContext db, CancellationToken cancellationToken = default)
    {
        var payments = await ListColumnsAsync(db, "Payments", cancellationToken);
        var courses = await ListColumnsAsync(db, "Courses", cancellationToken);
        var missingPayments = RequiredPaymentColumns.Where(c => !payments.Contains(c, StringComparer.Ordinal)).ToList();
        var missingCourses = RequiredCourseColumns.Where(c => !courses.Contains(c, StringComparer.Ordinal)).ToList();

        return new
        {
            appliedAt = LastAppliedAt,
            applyError = LastError,
            ready = missingPayments.Count == 0 && missingCourses.Count == 0 && LastError == null,
            paymentsColumns = payments,
            coursesColumns = courses,
            missingPayments,
            missingCourses
        };
    }

    private static async Task<List<string>> ListColumnsAsync(
        EducationalPlataformContext db,
        string table,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != System.Data.ConnectionState.Open;
        if (opened)
            await db.Database.OpenConnectionAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'public' AND table_name = @table
ORDER BY column_name
""";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "table";
            parameter.Value = table;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                names.Add(reader.GetString(0));
        }
        finally
        {
            if (opened)
                await db.Database.CloseConnectionAsync();
        }

        return names;
    }
}
