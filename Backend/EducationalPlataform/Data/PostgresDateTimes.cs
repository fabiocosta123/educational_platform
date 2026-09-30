using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Text;

namespace EducationalPlataform.Data;

public static class PostgresDateTimes
{
    public static DateTime Unspecified(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    public static DateTime? Unspecified(DateTime? value)
        => value.HasValue ? Unspecified(value.Value) : null;

    public static void NormalizeTracked(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dateTime)
                    property.CurrentValue = Unspecified(dateTime);
            }
        }
    }

    public static string Describe(Exception ex)
    {
        var parts = new List<string>();
        Collect(ex, parts);
        return parts.Count == 0
            ? "Não foi possível gravar no banco. Veja os logs da API."
            : string.Join(" | ", parts.Distinct());
    }

    public static string ForClient(Exception ex)
        => $"[{ex.GetType().Name}] {Describe(ex)}";

    public static bool IsMissingLateFeeColumn(Exception ex)
        => IsMissingColumn(ex);

    public static bool IsMissingColumn(Exception ex)
    {
        var text = Describe(ex);
        return text.Contains("42703")
            || text.Contains("does not exist", StringComparison.OrdinalIgnoreCase);
    }

    private static void Collect(Exception? ex, List<string> parts)
    {
        for (var current = ex; current != null; current = current.InnerException)
        {
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.Flatten().InnerExceptions)
                    Collect(inner, parts);
                break;
            }

            if (current is PostgresException postgres)
            {
                var text = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(postgres.SqlState))
                    text.Append(postgres.SqlState).Append(": ");
                text.Append(postgres.MessageText);
                if (!string.IsNullOrWhiteSpace(postgres.Detail))
                    text.Append(" ").Append(postgres.Detail);
                if (!string.IsNullOrWhiteSpace(postgres.Hint))
                    text.Append(" ").Append(postgres.Hint);
                if (!string.IsNullOrWhiteSpace(postgres.ColumnName))
                    text.Append(" Coluna: ").Append(postgres.ColumnName);
                if (!string.IsNullOrWhiteSpace(postgres.ConstraintName))
                    text.Append(" Constraint: ").Append(postgres.ConstraintName);
                parts.Add(text.ToString().Trim());
                continue;
            }

            if (string.IsNullOrWhiteSpace(current.Message))
                continue;

            if (current.Message.Contains("See the inner exception", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("target of an invocation", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            parts.Add(current.Message);
        }
    }

    public static string InnermostMessage(Exception ex) => Describe(ex);
}
