using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;

namespace api.Storage;

// Saves files into a folder in the company's SharePoint/OneDrive using Microsoft Graph.
// The API signs in as itself ("app-only"), using the app registration's Tenant ID,
// Client ID and Client Secret. Settings come from the "OneDrive" section of configuration.
public class OneDriveFileStorage : IFileStorage
{
    private const string GraphUrl = "https://graph.microsoft.com/v1.0";

    // Graph requires pieces that are a multiple of 320 KiB; 32 x 320 KiB = 10 MiB.
    private const int ChunkSize = 32 * 320 * 1024;

    private readonly HttpClient _http;
    private readonly TokenCredential? _credential;
    private readonly string _driveId;
    private readonly string _folderPath;

    public OneDriveFileStorage(IConfiguration config, IHttpClientFactory httpClientFactory)
    {
        var section = config.GetSection("OneDrive");
        var tenantId = section["TenantId"] ?? "";
        var clientId = section["ClientId"] ?? "";
        var clientSecret = section["ClientSecret"] ?? "";
        _driveId = section["DriveId"] ?? "";
        _folderPath = (section["FolderPath"] ?? "").Trim('/');
        _http = httpClientFactory.CreateClient();

        if (tenantId != "" && clientId != "" && clientSecret != "" && _driveId != "")
            _credential = new ClientSecretCredential(tenantId, clientId, clientSecret); // caches the sign-in token
    }

    public bool IsConfigured => _credential != null;

    public async Task<StoredFile> UploadAsync(Stream content, long length, string fileName, CancellationToken cancellationToken)
    {
        // Step 1: ask Graph for an upload session (a one-time address to send the file to).
        // "rename" means if the name is taken, OneDrive adds a number instead of overwriting.
        var name = $"{DateTime.UtcNow:yyyy-MM-dd} {SafeFileName(fileName)}";
        var path = string.Join("/", (_folderPath == "" ? name : _folderPath + "/" + name)
            .Split('/').Select(Uri.EscapeDataString));

        using var sessionRequest = await GraphRequest(HttpMethod.Post,
            $"{GraphUrl}/drives/{_driveId}/root:/{path}:/createUploadSession", cancellationToken);
        sessionRequest.Content = JsonContent.Create(new
        {
            item = new Dictionary<string, string> { ["@microsoft.graph.conflictBehavior"] = "rename" }
        });
        var session = await SendForJson(sessionRequest, cancellationToken);
        var uploadUrl = session.GetProperty("uploadUrl").GetString()!;

        // Step 2: send the file in 10 MiB pieces. The upload address already includes
        // permission, so these requests must NOT have the sign-in token attached.
        var buffer = new byte[ChunkSize];
        long offset = 0;
        JsonElement item = default;
        while (offset < length)
        {
            var read = await content.ReadAtLeastAsync(buffer, (int)Math.Min(ChunkSize, length - offset), false, cancellationToken);
            if (read == 0)
                throw new IOException("The file ended earlier than expected.");

            using var piece = new HttpRequestMessage(HttpMethod.Put, uploadUrl)
            {
                Content = new ByteArrayContent(buffer, 0, read)
            };
            piece.Content.Headers.ContentRange = new ContentRangeHeaderValue(offset, offset + read - 1, length);
            item = await SendForJson(piece, cancellationToken);
            offset += read;
        }

        // The last piece's reply describes the finished file.
        return new StoredFile(item.GetProperty("id").GetString()!, item.GetProperty("webUrl").GetString()!);
    }

    public async Task<string> GetContentUrlAsync(string fileId, CancellationToken cancellationToken)
    {
        using var request = await GraphRequest(HttpMethod.Get,
            $"{GraphUrl}/drives/{_driveId}/items/{Uri.EscapeDataString(fileId)}?select=id,@microsoft.graph.downloadUrl",
            cancellationToken);
        var item = await SendForJson(request, cancellationToken);
        return item.GetProperty("@microsoft.graph.downloadUrl").GetString()!;
    }

    public async Task DeleteAsync(string fileId, CancellationToken cancellationToken)
    {
        using var request = await GraphRequest(HttpMethod.Delete,
            $"{GraphUrl}/drives/{_driveId}/items/{Uri.EscapeDataString(fileId)}", cancellationToken);
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // A Graph request with the API's sign-in token attached.
    private async Task<HttpRequestMessage> GraphRequest(HttpMethod method, string url, CancellationToken cancellationToken)
    {
        if (_credential == null)
            throw new InvalidOperationException("OneDrive uploads are not set up.");

        var token = await _credential.GetTokenAsync(
            new TokenRequestContext(new[] { "https://graph.microsoft.com/.default" }), cancellationToken);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return request;
    }

    private async Task<JsonElement> SendForJson(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _http.SendAsync(request, cancellationToken);
        var text = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Microsoft Graph returned {(int)response.StatusCode}: {text}");
        return text == "" ? default : JsonDocument.Parse(text).RootElement.Clone();
    }

    // SharePoint doesn't allow these characters in file names.
    private static string SafeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        foreach (var c in "\"*:<>?/\\|")
            name = name.Replace(c, '_');
        name = name.Trim().TrimEnd('.');
        return name == "" ? "file" : name;
    }
}
