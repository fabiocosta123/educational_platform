using System.Globalization;
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

    public bool IsSandbox =>
        BaseUrl.Contains("sandbox", StringComparison.OrdinalIgnoreCase);

    private string BaseUrl => FirstValue(
            "MyCredit:BaseUrl",
            "MyCredit_BaseUrl",
            "MYCREDIT_BASEURL",
            "MyCredit__BaseUrl")
        .TrimEnd('/');

    private string Cnpj => Digits(FirstValue(
        "MyCredit:Cnpj",
        "MyCredit:CNPJ",
        "MyCredit_Cnpj",
        "MyCredit__Cnpj",
        "MYCREDIT_CNPJ"));

    private string ResellerToken => FirstValue(
            "MyCredit:ResellerToken",
            "MyCredit:Token",
            "MyCredit:ChaveIntegrador",
            "MyCredit:IntegratorKey",
            "MyCredit_ResellerToken",
            "MyCredit__ResellerToken",
            "MYCREDIT_RESELLERTOKEN",
            "CHAVE_INTEGRADOR")
        .Trim()
        .Trim('"')
        .Trim('\'');

    private string FirstValue(params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = _configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();

            var env = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim();
        }

        return "";
    }

    private string CnpjSource => SourceOf(
        "MyCredit:Cnpj",
        "MyCredit:CNPJ",
        "MyCredit_Cnpj",
        "MyCredit__Cnpj",
        "MYCREDIT_CNPJ");

    private string TokenSource => SourceOf(
        "MyCredit:ResellerToken",
        "MyCredit:Token",
        "MyCredit:ChaveIntegrador",
        "MyCredit:IntegratorKey",
        "MyCredit_ResellerToken",
        "MyCredit__ResellerToken",
        "MYCREDIT_RESELLERTOKEN",
        "CHAVE_INTEGRADOR");

    private string SourceOf(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!string.IsNullOrWhiteSpace(_configuration[key]) || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
                return key;
        }

        return "(nenhuma)";
    }

    public async Task<MyCreditCharge> CreatePixChargeAsync(
        string invoiceId,
        decimal amount,
        string payerName,
        string payerDocument,
        DateTime? dueDate,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetBearerTokenAsync(cancellationToken);
        HttpResponseMessage? response = null;
        string body = "";
        var currentInvoiceId = invoiceId;

        for (var attempt = 0; attempt < 2; attempt++)
        {
            var payloadJson = BuildPixPayloadJson(currentInvoiceId, amount, payerName);
            _logger.LogInformation(
                "MyCredit PIX payload invoice={Invoice} amount={Amount} payer={Payer}",
                currentInvoiceId,
                decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture),
                SanitizePayerName(payerName));

            response?.Dispose();
            response = await SendAuthorizedAsync(
                HttpMethod.Post,
                $"{BaseUrl}/api/pix",
                payloadJson,
                token,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                response.Dispose();
                InvalidateToken();
                token = await GetBearerTokenAsync(cancellationToken);
                response = await SendAuthorizedAsync(
                    HttpMethod.Post,
                    $"{BaseUrl}/api/pix",
                    payloadJson,
                    token,
                    cancellationToken);
            }

            body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
                break;

            _logger.LogWarning("MyCredit create PIX failed: {Status} {Body}", (int)response.StatusCode, body);
            var duplicate = attempt == 0
                && response.StatusCode == HttpStatusCode.BadRequest
                && body.Contains("idFaturaPag", StringComparison.OrdinalIgnoreCase);
            if (!duplicate)
            {
                throw new InvalidOperationException(
                    $"MyCredit {(int)response.StatusCode}: {ExtractError(body) ?? "A MyCredit recusou a cobrança PIX."} Payload: {RedactPayload(payloadJson)}");
            }

            currentInvoiceId = Guid.NewGuid().ToString();
        }

        try
        {
            if (response is null || !response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"MyCredit {(int?)response?.StatusCode}: {ExtractError(body) ?? "A MyCredit recusou a cobrança PIX."}");
            }

            var copyPaste = ReadRetUrl(body);
            if (string.IsNullOrWhiteSpace(copyPaste))
            {
                throw new InvalidOperationException(
                    $"MyCredit 200 sem código PIX: {(body.Length > 400 ? body[..400] : body)}");
            }

            var parsed = JsonSerializer.Deserialize<MyCreditEnvelope<MyCreditChargeData>>(body, JsonOptions);
            return new MyCreditCharge(
                copyPaste,
                parsed?.Data?.TransacaoId,
                parsed?.Data?.Expira,
                currentInvoiceId);
        }
        finally
        {
            response.Dispose();
        }
    }

    public async Task<bool> IsPaidAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetBearerTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/pix/{invoiceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await _http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Gone)
            return false;

        if (response.StatusCode == HttpStatusCode.NotFound)
            return false;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            InvalidateToken();
            return false;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("MyCredit PIX status failed: {Status} {Body}", (int)response.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
        return doc.RootElement.TryGetProperty("sucesso", out var sucesso)
               && sucesso.ValueKind == JsonValueKind.True;
    }

    public async Task RefundAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var token = await GetBearerTokenAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/api/pix/{invoiceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"MyCredit {(int)response.StatusCode}: {ExtractError(body) ?? "Não foi possível estornar o PIX."}");
        }
    }

    public async Task SimulatePaymentAsync(string invoiceId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        if (!IsSandbox)
        {
            throw new InvalidOperationException("A simulação de pagamento só existe no sandbox da MyCredit.");
        }

        var token = await GetBearerTokenAsync(cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{BaseUrl}/api/pix/simular-pagamento/{invoiceId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent("{}", Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"MyCredit {(int)response.StatusCode}: {ExtractError(body) ?? "A simulação de pagamento foi recusada."}");
        }
    }

    public async Task<MyCreditAuthProbe> ProbeAuthenticationAsync(CancellationToken cancellationToken = default)
    {
        var cnpjHint = Cnpj.Length >= 4 ? $"********{Cnpj[^4..]}" : "(vazio)";
        if (!IsConfigured)
        {
            return new MyCreditAuthProbe(
                false,
                false,
                BaseUrl,
                cnpjHint,
                Cnpj.Length,
                ResellerToken.Length,
                CnpjSource,
                TokenSource,
                null,
                "Variáveis MyCredit vazias neste processo. No Railway use MyCredit__Cnpj, MyCredit__ResellerToken e MyCredit__BaseUrl (dois underlines).");
        }

        try
        {
            InvalidateToken();
            var token = await GetBearerTokenAsync(cancellationToken);
            return new MyCreditAuthProbe(
                true,
                true,
                BaseUrl,
                cnpjHint,
                Cnpj.Length,
                token.Length,
                CnpjSource,
                TokenSource,
                200,
                "Token gerado com sucesso.");
        }
        catch (Exception ex)
        {
            return new MyCreditAuthProbe(
                true,
                false,
                BaseUrl,
                cnpjHint,
                Cnpj.Length,
                ResellerToken.Length,
                CnpjSource,
                TokenSource,
                null,
                ex.Message);
        }
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                "MyCredit não configurada. Defina MyCredit__BaseUrl, MyCredit__Cnpj e MyCredit__ResellerToken.");
        }
    }

    private void InvalidateToken()
    {
        _cachedToken = null;
        _tokenExpiresAt = default;
    }

    private async Task<string> GetBearerTokenAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
            return _cachedToken;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiresAt)
                return _cachedToken;

            _logger.LogInformation(
                "MyCredit using BaseUrl={BaseUrl} CnpjDigits={CnpjDigits} from {CnpjSource}, tokenLength={TokenLength} from {TokenSource}",
                BaseUrl,
                Cnpj.Length,
                CnpjSource,
                ResellerToken.Length,
                TokenSource);

            var secret = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Cnpj}|{ResellerToken}"));
            var url = BuildTokenUri(BaseUrl, secret);
            _logger.LogInformation(
                "MyCredit token GET path length {PathLength}, base64 hasSlash={HasSlash}",
                url.AbsolutePath.Length,
                secret.Contains('/'));
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await _http.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "MyCredit token failed: {Status} {Error}",
                    (int)response.StatusCode,
                    ExtractError(body) ?? body);
                throw new InvalidOperationException(
                    $"MyCredit token {(int)response.StatusCode}: {ExtractError(body) ?? "Não foi possível autenticar na MyCredit."}");
            }

            var token = ExtractToken(body);
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException(
                    $"A MyCredit não retornou o JWT em data. {(body.Length > 300 ? body[..300] : body)}");
            }

            _cachedToken = token;
            _tokenExpiresAt = DateTimeOffset.UtcNow.AddHours(24);
            _logger.LogInformation("MyCredit token obtained. Length {Length}.", token.Length);
            return token;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string url,
        string? json,
        string token,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (json != null)
        {
            request.Content = new StringContent(json, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        }

        return await _http.SendAsync(request, cancellationToken);
    }

    internal static string EncodeSecretForPath(string base64)
        => base64.Replace("+", "%2B").Replace("/", "%2F");

    /// <summary>
    /// HttpClient otherwise unescapes %2F back to /, splitting the Base64 secret into extra path segments.
    /// </summary>
    internal static Uri BuildTokenUri(string baseUrl, string base64Secret)
    {
        var encoded = EncodeSecretForPath(base64Secret);
        return new Uri(
            $"{baseUrl.TrimEnd('/')}/api/token/{encoded}",
            new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = true });
    }

    internal static string? FormatDueDate(DateTime? dueDate)
    {
        var today = BrasiliaToday();
        var date = dueDate?.Date;
        if (date is null || date.Value <= today)
            return null;
        return date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    internal static string SanitizePayerName(string? name)
    {
        var raw = (name ?? "").Trim();
        if (raw.Contains('@', StringComparison.Ordinal))
            raw = raw.Split('@')[0].Replace('.', ' ').Replace('_', ' ').Replace('-', ' ');

        var builder = new StringBuilder();
        var previousSpace = false;
        foreach (var c in raw)
        {
            if (char.IsLetter(c))
            {
                builder.Append(c);
                previousSpace = false;
            }
            else if (c is ' ' or '\'' && !previousSpace && builder.Length > 0)
            {
                builder.Append(' ');
                previousSpace = true;
            }
        }

        var cleaned = builder.ToString().Trim();
        if (cleaned.Length > 80)
            cleaned = cleaned[..80].Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
            return "Aluno Anexa";
        if (!cleaned.Contains(' '))
            cleaned += " Anexa";
        return cleaned;
    }

    /// <summary>
    /// Immediate PIX plus optional payer label. Document is initials + yyyyMMddHHmmss (Brasília),
    /// not the student's CPF, so the charge is identifiable in MyCredit without their EF client insert failing on CPF.
    /// </summary>
    internal static string BuildPixPayloadJson(string invoiceId, decimal amount, string payerName)
    {
        var valor = decimal.Round(amount, 2, MidpointRounding.AwayFromZero)
            .ToString("0.00", CultureInfo.InvariantCulture);
        var name = SanitizePayerName(payerName);
        var document = BuildPayerDocument(payerName, BrasiliaNow());
        return string.Concat(
            "{\"formaPagamento\":{\"tpTransacao\":11,\"idFaturaPag\":",
            JsonSerializer.Serialize(invoiceId),
            ",\"modPagamento\":18,\"valorPagamento\":",
            valor,
            "},\"cliente\":{\"xNome\":",
            JsonSerializer.Serialize(name),
            ",\"documento\":",
            JsonSerializer.Serialize(document),
            "}}");
    }

    internal static string BuildPayerDocument(string? name, DateTime when)
    {
        var initials = string.Concat(
            SanitizePayerName(name)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => char.ToLowerInvariant(part[0])));
        if (string.IsNullOrWhiteSpace(initials))
            initials = "al";
        return initials + when.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
    }

    private static DateTime BrasiliaToday()
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone).Date;
    }

    private static DateTime BrasiliaNow()
    {
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }

        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone);
    }

    private static string ExtractToken(string body)
    {
        var trimmed = body.Trim().Trim('"');
        if (!trimmed.StartsWith('{') && !trimmed.StartsWith('['))
            return trimmed;

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.String)
            return data.GetString() ?? "";

        return "";
    }

    private static string? ReadRetUrl(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var data)
                && data.ValueKind == JsonValueKind.Object
                && data.TryGetProperty("retUrl", out var retUrl)
                && retUrl.ValueKind == JsonValueKind.String)
            {
                return retUrl.GetString();
            }
        }
        catch (JsonException)
        {
        }

        return null;
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
            if (doc.RootElement.TryGetProperty("mensagem", out var mensagem) && mensagem.ValueKind == JsonValueKind.String)
                return mensagem.GetString();
            if (doc.RootElement.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                return message.GetString();
        }
        catch (JsonException)
        {
        }

        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static string RedactPayload(string json)
        => System.Text.RegularExpressions.Regex.Replace(
            json,
            "\"documento\":\"\\d+\"",
            "\"documento\":\"***\"");

    private static string Digits(string? value) => CpfValidator.DigitsOnly(value);
}

public sealed record MyCreditAuthProbe(
    bool Configured,
    bool TokenOk,
    string BaseUrl,
    string CnpjHint,
    int CnpjDigits,
    int TokenLength,
    string CnpjSource,
    string TokenSource,
    int? HttpStatus,
    string Message);

public sealed record MyCreditCharge(string CopyPaste, string? TransactionId, DateTime? ExpiresAt, string InvoiceId);

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
