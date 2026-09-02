using Npgsql;

namespace Planora.Infrastructure.Persistence;

public static class PostgreSqlConnectionString
{
    public static string Normalize(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("The PostgreSQL connection URI is invalid.", nameof(connectionString));
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.Trim('/'));
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(database))
        {
            throw new ArgumentException("The PostgreSQL connection URI must include credentials and a database name.", nameof(connectionString));
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Database = database,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
        };

        if (!uri.IsDefaultPort && uri.Port > 0)
        {
            builder.Port = uri.Port;
        }

        foreach (var pair in ParseQuery(uri.Query))
        {
            if (pair.Key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = ParseEnum<Npgsql.SslMode>(pair.Value, "SSL mode");
            }
            else if (pair.Key.Equals("channel_binding", StringComparison.OrdinalIgnoreCase))
            {
                builder.ChannelBinding = ParseEnum<ChannelBinding>(pair.Value, "channel binding mode");
            }
        }

        return builder.ConnectionString;
    }

    private static IEnumerable<KeyValuePair<string, string>> ParseQuery(string query)
    {
        foreach (var item in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = item.Split('=', 2);
            if (pair.Length == 2)
            {
                yield return new KeyValuePair<string, string>(
                    Uri.UnescapeDataString(pair[0]),
                    Uri.UnescapeDataString(pair[1]));
            }
        }
    }

    private static TEnum ParseEnum<TEnum>(string value, string settingName)
        where TEnum : struct, Enum
    {
        var normalized = value.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        if (Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var result))
        {
            return result;
        }

        throw new ArgumentException($"The PostgreSQL URI contains an unsupported {settingName}.");
    }
}
