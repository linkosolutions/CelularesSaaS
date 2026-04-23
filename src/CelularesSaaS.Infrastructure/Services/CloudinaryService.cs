using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace CelularesSaaS.Infrastructure.Services;

public interface ICloudinaryService
{
    Task<(string Url, string PublicId)> SubirImagenAsync(IFormFile archivo, string carpeta);
    Task EliminarImagenAsync(string publicId);
}

public class CloudinaryService : ICloudinaryService
{
    private readonly string _cloudName;
    private readonly string _apiKey;
    private readonly string _apiSecret;
    private readonly HttpClient _http;

    public CloudinaryService(IConfiguration config, IHttpClientFactory httpFactory)
    {
        _cloudName = config["Cloudinary:CloudName"]!;
        _apiKey = config["Cloudinary:ApiKey"]!;
        _apiSecret = config["Cloudinary:ApiSecret"]!;
        _http = httpFactory.CreateClient();
    }

    public async Task<(string Url, string PublicId)> SubirImagenAsync(IFormFile archivo, string carpeta)
    {
        using var ms = new MemoryStream();
        await archivo.CopyToAsync(ms);
        var bytes = ms.ToArray();
        var base64 = Convert.ToBase64String(bytes);
        var dataUri = $"data:{archivo.ContentType};base64,{base64}";

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var folder = $"celularessaas/{carpeta}";

        var toSign = $"folder={folder}&timestamp={timestamp}{_apiSecret}";
        var signature = ComputeSha1(toSign);

        var content = new MultipartFormDataContent
        {
            { new StringContent(dataUri),   "file" },
            { new StringContent(folder),    "folder" },
            { new StringContent(timestamp), "timestamp" },
            { new StringContent(_apiKey),   "api_key" },
            { new StringContent(signature), "signature" },
        };

        var response = await _http.PostAsync(
            $"https://api.cloudinary.com/v1_1/{_cloudName}/image/upload", content);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<CloudinaryResponse>();
        return (json!.SecureUrl, json.PublicId);
    }

    public async Task EliminarImagenAsync(string publicId)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var toSign = $"public_id={publicId}&timestamp={timestamp}{_apiSecret}";
        var signature = ComputeSha1(toSign);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["public_id"] = publicId,
            ["timestamp"] = timestamp,
            ["api_key"]   = _apiKey,
            ["signature"] = signature,
        });

        await _http.PostAsync(
            $"https://api.cloudinary.com/v1_1/{_cloudName}/image/destroy", content);
    }

    private static string ComputeSha1(string input)
    {
        var bytes = System.Security.Cryptography.SHA1.HashData(
            System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }

    private class CloudinaryResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("secure_url")]
        public string SecureUrl { get; set; } = null!;
        [System.Text.Json.Serialization.JsonPropertyName("public_id")]
        public string PublicId { get; set; } = null!;
    }
}
