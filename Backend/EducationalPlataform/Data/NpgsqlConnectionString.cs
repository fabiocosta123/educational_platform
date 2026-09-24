using System.Text;

namespace EducationalPlataform.Data;

public static class NpgsqlConnectionString
{
    public static string Normalize(string connectionString)
    {
        var value = connectionString.Trim().Trim('"');

        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (value.EndsWith("?sslmode", StringComparison.OrdinalIgnoreCase)
            || value.EndsWith("&sslmode", StringComparison.OrdinalIgnoreCase))
        {
            value += "=require";
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var host = uri.Host.Replace("-pooler", string.Empty, StringComparison.OrdinalIgnoreCase);
        var database = uri.AbsolutePath.Trim('/');
        var port = uri.IsDefaultPort ? 5432 : uri.Port;

        var builder = new StringBuilder();
        builder.Append("Host=").Append(host);
        builder.Append(";Port=").Append(port);
        builder.Append(";Database=").Append(database);
        builder.Append(";Username=").Append(username);
        builder.Append(";Password=").Append(password);
        builder.Append(";SSL Mode=Require;Trust Server Certificate=true");
        return builder.ToString();
    }
}
