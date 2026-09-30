namespace EducationalPlataform.Data;

public static class PostgresDateTimes
{
    public static DateTime Unspecified(DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

    public static DateTime? Unspecified(DateTime? value)
        => value.HasValue ? Unspecified(value.Value) : null;

    public static string InnermostMessage(Exception ex)
    {
        var current = ex;
        while (current.InnerException != null)
            current = current.InnerException;
        return current.Message;
    }
}
