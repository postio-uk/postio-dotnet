using System.Net;
using Postio.Sdk;
using Postio.Sdk.Models;
using RichardSzalay.MockHttp;
using Xunit;

namespace Postio.Sdk.Tests;

public class PostioClientTests
{
    private const string BaseUrl = "https://api.postio.co.uk/v1";

    private static (PostioClient Client, MockHttpMessageHandler Handler) NewClient(int retries = 0)
    {
        var handler = new MockHttpMessageHandler();
        var http = new HttpClient(handler);
        var options = new PostioClientOptions
        {
            ApiKey = "pk_test",
            Retries = retries,
            RetryBaseDelay = TimeSpan.Zero,
            RetryCapDelay = TimeSpan.Zero,
        };
        return (new PostioClient(options, http), handler);
    }

    [Fact]
    public async Task Search_ReturnsTypedEnvelope()
    {
        var (client, handler) = NewClient();
        handler.When(HttpMethod.Get, $"{BaseUrl}/address/search")
               .WithQueryString("q", "downing")
               .Respond("application/json",
                   """
                   {"success":true,"results":[{"udprn":12345,"suggestion":"10 Downing Street"}],
                    "meta":{"countResults":1,"requestId":"abc-123","performance":{"workerMs":10,"lookupMs":5}}}
                   """);

        var result = await client.Address.SearchAsync("downing", maxResults: 5);

        Assert.True(result.Success);
        Assert.Single(result.Results);
        Assert.Equal(12345, result.Results[0].Udprn);
        Assert.Equal("abc-123", result.Meta.RequestId);
    }

    [Fact]
    public async Task Status401_ThrowsInvalidKey()
    {
        var (client, handler) = NewClient();
        handler.When(HttpMethod.Get, $"{BaseUrl}/connect")
               .Respond(HttpStatusCode.Unauthorized, "application/json",
                   """
                   {"success":false,"error":"invalid_api_key","details":"Key not recognised","results":[],
                    "meta":{"countResults":0,"requestId":"r-401","performance":{"workerMs":1,"lookupMs":0}}}
                   """);

        var ex = await Assert.ThrowsAsync<PostioInvalidKeyException>(() => client.ConnectAsync());
        Assert.Equal(401, ex.Status);
        Assert.Equal("invalid_api_key", ex.ErrorCode);
        Assert.Equal("Key not recognised", ex.Details);
        Assert.Equal("r-401", ex.RequestId);
    }

    [Fact]
    public async Task Status402_ThrowsOutOfCredit()
    {
        var (client, handler) = NewClient();
        handler.When(HttpMethod.Get, $"{BaseUrl}/connect")
               .Respond(HttpStatusCode.PaymentRequired, "application/json",
                   """
                   {"success":false,"error":"out_of_credit","results":[],
                    "meta":{"countResults":0,"requestId":"r-402","performance":{"workerMs":1,"lookupMs":0}}}
                   """);

        await Assert.ThrowsAsync<PostioOutOfCreditException>(() => client.ConnectAsync());
    }

    [Fact]
    public async Task Status429_SurfacesRetryAfter()
    {
        var (client, handler) = NewClient();
        handler.When(HttpMethod.Get, $"{BaseUrl}/connect")
               .Respond(req =>
               {
                   var resp = new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                   {
                       Content = new StringContent(
                           """
                           {"success":false,"error":"rate_limited","results":[],
                            "meta":{"countResults":0,"requestId":"r-429","performance":{"workerMs":1,"lookupMs":0}}}
                           """,
                           System.Text.Encoding.UTF8, "application/json"),
                   };
                   resp.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(12));
                   return resp;
               });

        var ex = await Assert.ThrowsAsync<PostioRateLimitException>(() => client.ConnectAsync());
        Assert.Equal(12, ex.RetryAfter);
    }

    [Fact]
    public async Task Status5xx_RetriesThenSucceeds()
    {
        var (client, handler) = NewClient(retries: 2);
        var calls = 0;
        handler.When(HttpMethod.Get, $"{BaseUrl}/connect").Respond(req =>
        {
            calls++;
            if (calls == 1)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent(
                        """
                        {"success":false,"error":"unavailable","results":[],
                         "meta":{"countResults":0,"requestId":"r1","performance":{"workerMs":1,"lookupMs":0}}}
                        """,
                        System.Text.Encoding.UTF8, "application/json"),
                };
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"success":true,"meta":{"requestId":"r-ok","performance":{"workerMs":5,"lookupMs":2}}}
                    """,
                    System.Text.Encoding.UTF8, "application/json"),
            };
        });

        var r = await client.ConnectAsync();
        Assert.True(r.Success);
        Assert.Equal("r-ok", r.Meta.RequestId);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task Status5xx_ExhaustedThrowsServerError()
    {
        var (client, _) = NewClient(retries: 1);
        var ex = await Assert.ThrowsAsync<PostioServerException>(async () =>
        {
            using var realHandler = new MockHttpMessageHandler();
            realHandler.When(HttpMethod.Get, $"{BaseUrl}/connect").Respond(HttpStatusCode.InternalServerError, "application/json",
                """
                {"success":false,"error":"internal","results":[],
                 "meta":{"countResults":0,"requestId":"r-500","performance":{"workerMs":1,"lookupMs":0}}}
                """);
            using var http = new HttpClient(realHandler);
            using var c = new PostioClient(new PostioClientOptions
            {
                ApiKey = "pk_test",
                Retries = 1,
                RetryBaseDelay = TimeSpan.Zero,
                RetryCapDelay = TimeSpan.Zero,
            }, http);
            await c.ConnectAsync();
        });
        Assert.Equal(500, ex.Status);
    }

    [Fact]
    public void Constructor_RequiresApiKey()
    {
        Environment.SetEnvironmentVariable("POSTIO_API_KEY", null);
        Assert.Throws<ArgumentException>(() => new PostioClient(new PostioClientOptions()));
    }

    [Fact]
    public async Task UserAgent_StartsWithSdkPrefix()
    {
        var (client, handler) = NewClient();
        string? captured = null;
        handler.When(HttpMethod.Get, $"{BaseUrl}/connect").Respond(req =>
        {
            captured = req.Headers.UserAgent.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {"success":true,"meta":{"requestId":"r","performance":{"workerMs":1,"lookupMs":0}}}
                    """,
                    System.Text.Encoding.UTF8, "application/json"),
            };
        });
        await client.ConnectAsync();
        Assert.NotNull(captured);
        Assert.StartsWith("postio-dotnet/", captured);
    }
}
