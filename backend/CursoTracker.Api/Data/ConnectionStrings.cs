using Npgsql;

namespace CursoTracker.Api.Data;

public static class ConnectionStrings
{
    /// <summary>
    /// Aceita o formato do Npgsql ou a URL que o Neon/Render mostram
    /// (<c>postgresql://usuario:senha@host/banco?sslmode=require</c>).
    /// </summary>
    public static string ToNpgsql(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return value;

        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port < 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase) && parts.Length == 2)
                builder.SslMode = Enum.Parse<SslMode>(parts[1], ignoreCase: true);
        }
        return builder.ConnectionString;
    }
}
