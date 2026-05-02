using Xunit;
using Postio.Sdk;

namespace Postio.Sdk.Tests;

/// <summary>
/// Live tests against api.postio.co.uk (or stage). Tagged with
/// Trait("Category", "Live") so the offline CI run can filter them
/// out via `dotnet test --filter Category!=Live`. The live job in CI
/// runs everything (no filter) with the secret in env.
///
/// Skipped when no POSTIO_API_KEY* env var is in scope.
/// </summary>
[Trait("Category", "Live")]
public class LiveTests
{
    private static (PostioClient Client, bool Skip) NewClient()
    {
        if (Environment.GetEnvironmentVariable("POSTIO_API_KEY_STAGE") is { Length: > 0 } stage)
        {
            return (new PostioClient(new PostioClientOptions
            {
                ApiKey = stage,
                BaseUrl = "https://stage-api.postio.co.uk/v1",
            }), false);
        }
        if (Environment.GetEnvironmentVariable("POSTIO_API_KEY_PROD") is { Length: > 0 } prod)
            return (new PostioClient(prod), false);
        if (Environment.GetEnvironmentVariable("POSTIO_API_KEY") is { Length: > 0 } any)
            return (new PostioClient(any), false);
        return (null!, true);
    }

    [Fact]
    public async Task Connect()
    {
        var (client, skip) = NewClient();
        if (skip) return;
        using var c = client;
        var r = await c.ConnectAsync();
        Assert.True(r.Success);
        Assert.False(string.IsNullOrEmpty(r.Meta.RequestId));
    }

    [Fact]
    public async Task AddressSearch()
    {
        var (client, skip) = NewClient();
        if (skip) return;
        using var c = client;
        var r = await c.Address.SearchAsync("downing street", maxResults: 3);
        Assert.True(r.Success);
        Assert.NotEmpty(r.Results);
        Assert.True(r.Results[0].Udprn > 0);
    }

    [Fact]
    public async Task EmailValidate()
    {
        var (client, skip) = NewClient();
        if (skip) return;
        using var c = client;
        var r = await c.Email.ValidateAsync("admin@postio.co.uk");
        Assert.True(r.Success);
        Assert.Single(r.Results);
        Assert.True(r.Results[0].IsValidSyntax);
    }

    [Fact]
    public async Task PhoneValidate()
    {
        var (client, skip) = NewClient();
        if (skip) return;
        using var c = client;
        var r = await c.Phone.ValidateAsync("+442079460000");
        Assert.True(r.Success);
        Assert.Single(r.Results);
        Assert.True(r.Results[0].IsValid);
    }
}
