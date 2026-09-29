using System.Net.Http.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EducationalPlataform.Validation;

namespace EducationalPlataform.Services;

public sealed class MyCreditClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MyCreditClient> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiresAt;

    public MyCreditClient(HttpClient http, IConfiguration configuration, ILogger<MyCreditClient> logger)
    {
        _http = http;
        _configuration = configuration;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Cnpj)
        && !string.IsNullOrWhiteSpace(ResellerToken);

    private string BaseUrl => (_configuration["MyCredit:BaseUrl"] ?? "").TrimEnd('/');
    private string Cnpj => Digits(_configuration["MyCredit:Cnpj"]);
    private string ResellerToken => (_configuration["MyCredit:ResellerToken"] ?? "")
        .Trim()
        .Trim('"')
        .Trim('\'');

    public async Task<MyCreditCharge> CreatePixChargeAsync(
        string invoiceId,
        decimal amount,
        string payerName,
        string payerDocument,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetBearerTokenAsync(cancellationToken);

        var payload = new
        {
            formaPagamento = new
            {
                tpTransacao = 11,
                idFaturaPag = invoiceId,
                modPagamento = 18,
                qtdParcelas = 1,
                valorPagamento = amount
            },
            cliente = new
            {
                xNome = payerName,
                documento = Digits(payerDocument)
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}/api/pix")
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MyCredit create PIX failed: {Status} {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(ExtractError(body) ?? "A MyCredit recusou a cobrança PIX.");
        }

        var parsed = JsonSerializer.Deserialize<MyCreditEnvelope<MyCreditChargeData>>(body, JsonOptions);
        var copyPaste = parsed?.Data?.RetUrl;
        if (string.IsNullOrWhiteSpace(copyPaste))
        {
            throw new InvalidOperationException("A MyCredit não retornou o código PIX.");
        }

        return new MyCreditCharge(
            copyPaste,
            parsed?.Data?.TransacaoId,
            parsed?.Data?.Expira);
    }

    public async Task<bool> IsPaidAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetBearerTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/pix/{invoiceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Gone)
        {
            return false;
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MyCredit PIX status failed: {Status} {Body}", (int)response.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        if (doc.RootElement.TryGetProperty("sucesso", out var sucesso) && sucesso.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        return false;
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "MyCredit não configurada. Defina MyCredit__BaseUrl, MyCredit__Cnpj e MyCredit__ResellerToken.");
        }
    }

    private async Task<string> GetBearerTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
            {
                return _cachedToken;
            }

            var secret = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Cnpj}|{ResellerToken}"));
            var url = $"{BaseUrl}/api/token/{Uri.EscapeDataString(secret)}";
            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(ExtractError(body) ?? "Não foi possível autenticar na MyCredit.");
            }

            var token = ExtractToken(body);
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("A MyCredit não retornou o Bearer Token.");
            }

            _cachedToken = token;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddMinutes(50);
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private static string ExtractToken(string body)
    {
        var trimmed = body.Trim().Trim('"');
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
        {
            return trimmed;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        foreach (var name in new[] { "token", "access_token", "accessToken", "bearerToken" })
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? "";
            }
        }

        if (root.TryGetProperty("data", out var data))
        {
            if (data.ValueKind == JsonValueKind.String)
            {
                return data.GetString() ?? "";
            }

            foreach (var name in new[] { "token", "access_token", "accessToken" })
            {
                if (data.TryGetProperty(name, out var nested) && nested.ValueKind == JsonValueKind.String)
                {
                    return nested.GetString() ?? "";
                }
            }
        }

        return "";
    }

    private static string? ExtractError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("errors", out var errors))
            {
                return errors.ValueKind == JsonValueKind.String
                    ? errors.GetString()
                    : errors.ToString();
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static string Digits(string? value) => CpfValidator.DigitsOnly(value);
}

public sealed record MyCreditCharge(string CopyPaste, string? TransactionId, DateTime? ExpiresAt);

file sealed class MyCreditEnvelope<T>
{
    public bool Sucesso { get; set; }
    public T? Data { get; set; }
}

file sealed class MyCreditChargeData
{
    [JsonPropertyName("retUrl")]
    public string? RetUrl { get; set; }

    [JsonPropertyName("transacaoId")]
    public string? TransacaoId { get; set; }

    [JsonPropertyName("expira")]
    public DateTime? Expira { get; set; }
}
