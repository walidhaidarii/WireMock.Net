using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using WireMock.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;
using Xunit;

namespace WireMock.Net.Tests;

public class WireMockServerWebhookTests
{
    [Fact]
    public async Task WireMockServer_WithWebhooks_Should_Send_Message_To_Webhooks()
    {
        // Assign
        var serverReceivingTheWebhook1 = WireMockServer.Start();
        serverReceivingTheWebhook1.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(200));

        var serverReceivingTheWebhook2 = WireMockServer.Start();
        serverReceivingTheWebhook2.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(200));

        var webhook1 = new Webhook
        {
            Request = new WebhookRequest
            {
                Url = serverReceivingTheWebhook1.Urls[0],
                Method = "post",
                BodyData = new BodyData
                {
                    BodyAsString = "1",
                    DetectedBodyType = BodyType.String,
                    DetectedBodyTypeFromContentType = BodyType.String
                }
            }
        };

        var webhook2 = new Webhook
        {
            Request = new WebhookRequest
            {
                Url = serverReceivingTheWebhook2.Urls[0],
                Method = "post",
                BodyData = new BodyData
                {
                    BodyAsString = "2",
                    DetectedBodyType = BodyType.String,
                    DetectedBodyTypeFromContentType = BodyType.String
                }
            }
        };

        // Act
        var server = WireMockServer.Start();
        server.Given(Request.Create().UsingPost())
            .WithWebhook(webhook1, webhook2)
            .RespondWith(Response.Create().WithBody("a-response"));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{server.Urls[0]}/TST"),
            Content = new StringContent("test")
        };

        // Assert
        var response = await new HttpClient().SendAsync(request).ConfigureAwait(false);
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("a-response");

        serverReceivingTheWebhook1.LogEntries.Should().HaveCount(1);
        serverReceivingTheWebhook2.LogEntries.Should().HaveCount(1);

        server.Dispose();
        serverReceivingTheWebhook1.Dispose();
        serverReceivingTheWebhook2.Dispose();
    }

    [Fact]
    public async Task WireMockServer_WithWebhook_Should_Send_Message_To_Webhook()
    {
        // Assign
        var serverReceivingTheWebhook = WireMockServer.Start();
        serverReceivingTheWebhook.Given(Request.Create().WithPath("/x").UsingPost()).RespondWith(Response.Create().WithStatusCode(200));

        // Act
        var server = WireMockServer.Start();
        server.Given(Request.Create().UsingPost())
            .WithWebhook(new Webhook
            {
                Request = new WebhookRequest
                {
                    Url = serverReceivingTheWebhook.Url! + "/{{request.Query.q}}",
                    Method = "post",
                    BodyData = new BodyData
                    {
                        BodyAsString = "abc",
                        DetectedBodyType = BodyType.String,
                        DetectedBodyTypeFromContentType = BodyType.String
                    },
                    UseTransformer = true
                }
            })
            .RespondWith(Response.Create().WithBody("a-response"));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{server.Urls[0]}/TST?q=x"),
            Content = new StringContent("test")
        };

        // Assert
        var response = await new HttpClient().SendAsync(request).ConfigureAwait(false);
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("a-response");

        serverReceivingTheWebhook.LogEntries.Should().HaveCount(1);
        serverReceivingTheWebhook.LogEntries.First().MappingGuid.Should().NotBeNull();

        server.Dispose();
        serverReceivingTheWebhook.Dispose();
    }

    [Fact]
    public async Task WireMockServer_WithWebhookArgs_Should_Send_StringMessage_To_Webhook()
    {
        // Assign
        var serverReceivingTheWebhook = WireMockServer.Start();
        serverReceivingTheWebhook.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(200));

        // Act
        var server = WireMockServer.Start();
        server.Given(Request.Create().UsingPost())
            .WithWebhook(serverReceivingTheWebhook.Urls[0], "post", null, "OK !", true, TransformerType.Handlebars)
            .RespondWith(Response.Create().WithBody("a-response"));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{server.Urls[0]}/TST"),
            Content = new StringContent("test")
        };

        // Assert
        var response = await new HttpClient().SendAsync(request).ConfigureAwait(false);
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("a-response");

        serverReceivingTheWebhook.LogEntries.Should().HaveCount(1);
        serverReceivingTheWebhook.LogEntries.First().RequestMessage.Body.Should().Be("OK !");

        server.Dispose();
        serverReceivingTheWebhook.Dispose();
    }

    [Fact]
    public async Task WireMockServer_WithWebhookArgs_Should_Send_JsonMessage_To_Webhook()
    {
        // Assign
        var serverReceivingTheWebhook = WireMockServer.Start();
        serverReceivingTheWebhook.Given(Request.Create().UsingPost()).RespondWith(Response.Create().WithStatusCode(200));

        // Act
        var server = WireMockServer.Start();
        server.Given(Request.Create().UsingPost())
            .WithWebhook(serverReceivingTheWebhook.Urls[0], "post", null, new { Status = "OK" }, true, TransformerType.Handlebars)
            .RespondWith(Response.Create().WithBody("a-response"));

        var request = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri($"{server.Urls[0]}/TST"),
            Content = new StringContent("test")
        };

        // Assert
        var response = await new HttpClient().SendAsync(request).ConfigureAwait(false);
        string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("a-response");

        serverReceivingTheWebhook.LogEntries.Should().HaveCount(1);
        serverReceivingTheWebhook.LogEntries.First().RequestMessage.Body.Should().Be("{\"Status\":\"OK\"}");

        server.Dispose();
        serverReceivingTheWebhook.Dispose();
    }
}