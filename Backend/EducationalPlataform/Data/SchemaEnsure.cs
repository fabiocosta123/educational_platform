using Microsoft.EntityFrameworkCore;

namespace EducationalPlataform.Data;

public static class SchemaEnsure
{
    public const string Sql = """
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "SettledAt" timestamp without time zone NULL;
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "InstallmentNumber" integer NULL;
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "LateFeeApplied" boolean NOT NULL DEFAULT false;
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixInvoiceId" text NULL;
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixTransactionId" text NULL;
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixCopyPaste" text NULL;
ALTER TABLE "Payments" ADD COLUMN IF NOT EXISTS "PixExpiresAt" timestamp without time zone NULL;
ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "Price" numeric(10,2) NOT NULL DEFAULT 0;
ALTER TABLE "Courses" ADD COLUMN IF NOT EXISTS "InstallmentCount" integer NOT NULL DEFAULT 12;
""";

    public static Task ApplyAsync(EducationalPlataformContext db, CancellationToken cancellationToken = default)
        => db.Database.ExecuteSqlRawAsync(Sql, cancellationToken);

    public static void Apply(EducationalPlataformContext db)
        => db.Database.ExecuteSqlRaw(Sql);
}
