using CursoTracker.Api.Data;
using Npgsql;

namespace CursoTracker.Api.Tests;

public class ConnectionStringsTests
{
    [Fact]
    public void Converts_the_neon_url()
    {
        var result = new NpgsqlConnectionStringBuilder(ConnectionStrings.ToNpgsql(
            "postgresql://bruno:s3nh%40forte@ep-cool-name-123.sa-east-1.aws.neon.tech/neondb?sslmode=require"));

        Assert.Equal("ep-cool-name-123.sa-east-1.aws.neon.tech", result.Host);
        Assert.Equal(5432, result.Port);
        Assert.Equal("neondb", result.Database);
        Assert.Equal("bruno", result.Username);
        Assert.Equal("s3nh@forte", result.Password);
        Assert.Equal(SslMode.Require, result.SslMode);
    }

    [Fact]
    public void Keeps_npgsql_format_untouched()
    {
        const string value = "Host=localhost;Database=cursotracker;Username=postgres;Password=postgres";

        Assert.Equal(value, ConnectionStrings.ToNpgsql(value));
    }
}
