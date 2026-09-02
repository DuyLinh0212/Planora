using Npgsql;
using Planora.Infrastructure.Persistence;

namespace Planora.IntegrationTests;

public sealed class PostgreSqlConnectionStringTests
{
    [Fact]
    public void Normalize_NeonUri_ProducesNpgsqlConnectionString()
    {
        const string uri = "postgresql://planora%2Downer:p%40ss%3Aword@ep-example-pooler.us-east-2.aws.neon.tech/planora?sslmode=require&channel_binding=require";

        var result = PostgreSqlConnectionString.Normalize(uri);
        var parsed = new NpgsqlConnectionStringBuilder(result);

        Assert.Equal("ep-example-pooler.us-east-2.aws.neon.tech", parsed.Host);
        Assert.Equal("planora-owner", parsed.Username);
        Assert.Equal("p@ss:word", parsed.Password);
        Assert.Equal("planora", parsed.Database);
        Assert.Equal(SslMode.Require, parsed.SslMode);
        Assert.Equal(ChannelBinding.Require, parsed.ChannelBinding);
    }

    [Fact]
    public void Normalize_NpgsqlConnectionString_ReturnsItUnchanged()
    {
        const string connectionString = "Host=localhost;Database=planora;Username=postgres;Password=postgres";

        Assert.Equal(connectionString, PostgreSqlConnectionString.Normalize(connectionString));
    }
}
