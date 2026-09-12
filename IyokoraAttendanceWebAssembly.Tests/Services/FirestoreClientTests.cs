using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using IyokoraAttendanceWebAssembly.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace IyokoraAttendanceWebAssembly.Tests.Services;

public class FirestoreClientTests
{
    private static (FirestoreClient client, Mock<HttpMessageHandler> handler) CreateClient(string? appCheckToken = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://firestore.googleapis.com/") };

        var appCheckMock = new Mock<IAppCheckTokenProvider>();
        appCheckMock.Setup(a => a.GetTokenAsync()).ReturnsAsync(appCheckToken);

        return (new FirestoreClient(httpClient, NullLogger<FirestoreClient>.Instance, appCheckMock.Object), handlerMock);
    }

    private static void SetupResponse(Mock<HttpMessageHandler> handler, HttpMethod method, string urlContains, HttpStatusCode statusCode, string? jsonBody = null)
    {
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == method && r.RequestUri!.ToString().Contains(urlContains)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = jsonBody is null ? null : new StringContent(jsonBody, Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public async Task ドキュメント一覧取得で複数のフィールド型が正しく解析される()
    {
        var (client, handler) = CreateClient();
        var json = """
        {
          "documents": [
            {
              "name": "projects/p/databases/(default)/documents/pieces/piece1",
              "fields": {
                "title": { "stringValue": "曲A" },
                "count": { "integerValue": "3" },
                "isArchived": { "booleanValue": false },
                "createdAt": { "timestampValue": "2026-01-02T03:04:05.000Z" }
              }
            }
          ]
        }
        """;
        SetupResponse(handler, HttpMethod.Get, "pieces", HttpStatusCode.OK, json);

        var docs = await client.ListDocumentsAsync("pieces");

        Assert.Single(docs);
        var doc = docs[0];
        Assert.Equal("piece1", doc.Id);
        Assert.Equal("曲A", doc.GetString("title"));
        Assert.Equal(3, doc.GetLong("count"));
        Assert.False(doc.GetBool("isArchived"));
        Assert.Equal(new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), doc.GetDateTime("createdAt"));
    }

    [Fact]
    public async Task コレクションが存在しない場合は空リストが返る()
    {
        var (client, handler) = CreateClient();
        SetupResponse(handler, HttpMethod.Get, "pieces", HttpStatusCode.NotFound);

        var docs = await client.ListDocumentsAsync("pieces");

        Assert.Empty(docs);
    }

    [Fact]
    public async Task 数値として解析できないintegerValueは例外を投げずフォールバック値になる()
    {
        var (client, handler) = CreateClient();
        var json = """
        {
          "documents": [
            {
              "name": "projects/p/databases/(default)/documents/pieces/piece1",
              "fields": {
                "count": { "integerValue": "not-a-number" }
              }
            }
          ]
        }
        """;
        SetupResponse(handler, HttpMethod.Get, "pieces", HttpStatusCode.OK, json);

        var docs = await client.ListDocumentsAsync("pieces");

        Assert.Equal(0, docs[0].GetLong("count"));
    }

    [Fact]
    public async Task 日時として解析できないtimestampValueは例外を投げずフォールバック値になる()
    {
        var (client, handler) = CreateClient();
        var json = """
        {
          "documents": [
            {
              "name": "projects/p/databases/(default)/documents/pieces/piece1",
              "fields": {
                "createdAt": { "timestampValue": "not-a-date" }
              }
            }
          ]
        }
        """;
        SetupResponse(handler, HttpMethod.Get, "pieces", HttpStatusCode.OK, json);

        var docs = await client.ListDocumentsAsync("pieces");

        Assert.Equal(default, docs[0].GetDateTime("createdAt"));
    }

    [Fact]
    public async Task ページングされた複数ページのドキュメントがすべて取得される()
    {
        var (client, handler) = CreateClient();
        var page1 = """
        {
          "documents": [ { "name": "projects/p/databases/(default)/documents/pieces/piece1", "fields": {} } ],
          "nextPageToken": "TOKEN1"
        }
        """;
        var page2 = """
        {
          "documents": [ { "name": "projects/p/databases/(default)/documents/pieces/piece2", "fields": {} } ]
        }
        """;

        handler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page1, Encoding.UTF8, "application/json") })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(page2, Encoding.UTF8, "application/json") });

        var docs = await client.ListDocumentsAsync("pieces");

        Assert.Equal(["piece1", "piece2"], docs.Select(d => d.Id));
    }

    [Fact]
    public async Task 存在しないドキュメントを取得するとnullが返る()
    {
        var (client, handler) = CreateClient();
        SetupResponse(handler, HttpMethod.Get, "pieces/missing", HttpStatusCode.NotFound);

        var doc = await client.GetDocumentAsync("pieces", "missing");

        Assert.Null(doc);
    }

    [Fact]
    public async Task 存在するドキュメントを取得すると内容が解析される()
    {
        var (client, handler) = CreateClient();
        var json = """
        {
          "name": "projects/p/databases/(default)/documents/pieces/piece1",
          "fields": { "title": { "stringValue": "曲A" } }
        }
        """;
        SetupResponse(handler, HttpMethod.Get, "pieces/piece1", HttpStatusCode.OK, json);

        var doc = await client.GetDocumentAsync("pieces", "piece1");

        Assert.NotNull(doc);
        Assert.Equal("曲A", doc!.GetString("title"));
    }

    [Fact]
    public async Task ドキュメント保存でPATCHリクエストにフィールドとupdateMaskが含まれる()
    {
        var (client, handler) = CreateClient();
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) =>
            {
                capturedRequest = req;
                capturedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        await client.UpsertDocumentAsync("pieces", "piece1", new Dictionary<string, object?>
        {
            ["title"] = "曲A",
            ["isArchived"] = true
        });

        Assert.Equal(HttpMethod.Patch, capturedRequest!.Method);
        Assert.Contains("updateMask.fieldPaths=title", capturedRequest.RequestUri!.Query);
        Assert.Contains("updateMask.fieldPaths=isArchived", capturedRequest.RequestUri.Query);

        var body = JsonNode.Parse(capturedBody!)!;
        Assert.Equal("曲A", body["fields"]!["title"]!["stringValue"]!.GetValue<string>());
        Assert.True(body["fields"]!["isArchived"]!["booleanValue"]!.GetValue<bool>());
    }

    [Fact]
    public async Task ドキュメント削除で存在しない場合は例外にならない()
    {
        var (client, handler) = CreateClient();
        SetupResponse(handler, HttpMethod.Delete, "pieces/missing", HttpStatusCode.NotFound);

        await client.DeleteDocumentAsync("pieces", "missing");
    }

    [Fact]
    public async Task ドキュメント削除で存在する場合は正常に完了する()
    {
        var (client, handler) = CreateClient();
        SetupResponse(handler, HttpMethod.Delete, "pieces/piece1", HttpStatusCode.OK);

        await client.DeleteDocumentAsync("pieces", "piece1");
    }

    [Fact]
    public async Task AppCheckトークンが取得できる場合はリクエストヘッダーに付与される()
    {
        var (client, handler) = CreateClient(appCheckToken: "dummy-token");
        HttpRequestMessage? capturedRequest = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });

        await client.GetDocumentAsync("pieces", "piece1");

        Assert.Equal("dummy-token", capturedRequest!.Headers.GetValues("X-Firebase-AppCheck").Single());
    }

    [Fact]
    public async Task AppCheckトークンが取得できない場合はヘッダーを付与せずに送信される()
    {
        var (client, handler) = CreateClient(appCheckToken: null);
        HttpRequestMessage? capturedRequest = null;

        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}", Encoding.UTF8, "application/json") });

        await client.GetDocumentAsync("pieces", "piece1");

        Assert.False(capturedRequest!.Headers.Contains("X-Firebase-AppCheck"));
    }
}
