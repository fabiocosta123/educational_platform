using System.Collections.Concurrent;
using System.Net.Mail;
using System.Text.Json;

namespace EducationalPlataform.Services;

public sealed class EmailDeliverabilityService
{
    private static readonly ConcurrentDictionary<string, CacheEntry> MxCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> BlockedDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "example.com", "example.org", "example.net", "test.com", "test.org",
        "localhost", "invalid", "mailinator.com", "10minutemail.com", "guerrillamail.com",
        "tempmail.com", "trashmail.com", "yopmail.com", "sharklasers.com"
    };

    private readonly HttpClient _http;
    private readonly ILogger<EmailDeliverabilityService> _logger;

    public EmailDeliverabilityService(HttpClient http, ILogger<EmailDeliverabilityService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string?> ValidateAsync(string? email, CancellationToken cancellationToken = default)
    {
        var formatError = ValidateFormat(email);
        if (formatError != null)
            return formatError;

        var domain = new MailAddress(email!.Trim()).Host;
        if (BlockedDomains.Contains(domain))
            return "Este e-mail não é válido. Use um endereço real, como Gmail ou Outlook.";

        try
        {
            if (!await HasMxAsync(domain, cancellationToken))
            {
                return "Este e-mail não existe. Informe um endereço real que receba mensagens.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao consultar MX de {Domain}", domain);
            return "Não foi possível verificar o e-mail agora. Tente de novo em instantes.";
        }

        return null;
    }

    public static string? ValidateFormat(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Informe um e-mail.";

        var value = email.Trim();
        if (value.Contains(' ', StringComparison.Ordinal) || value.Contains("..", StringComparison.Ordinal))
            return "E-mail inválido.";

        MailAddress address;
        try
        {
            address = new MailAddress(value);
        }
        catch (FormatException)
        {
            return "E-mail inválido.";
        }

        if (!string.Equals(address.Address, value, StringComparison.OrdinalIgnoreCase))
            return "E-mail inválido.";

        var domain = address.Host;
        var dot = domain.LastIndexOf('.');
        if (dot < 1 || dot == domain.Length - 1)
            return "E-mail inválido.";

        var tld = domain[(dot + 1)..];
        if (tld.Length < 2 || tld.Any(ch => !char.IsLetter(ch)))
            return "E-mail inválido.";

        return null;
    }

    private async Task<bool> HasMxAsync(string domain, CancellationToken cancellationToken)
    {
        if (MxCache.TryGetValue(domain, out var cached) && cached.ExpiresAt > DateTimeOffset.UtcNow)
            return cached.HasMx;

        var url = $"resolve?name={Uri.EscapeDataString(domain)}&type=MX";
        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = doc.RootElement;
        var status = root.TryGetProperty("Status", out var statusEl) ? statusEl.GetInt32() : -1;
        var hasMx = status == 0
            && root.TryGetProperty("Answer", out var answer)
            && answer.ValueKind == JsonValueKind.Array
            && answer.EnumerateArray().Any(item =>
                item.TryGetProperty("type", out var type) && type.GetInt32() == 15);

        MxCache[domain] = new CacheEntry(hasMx, DateTimeOffset.UtcNow.AddHours(6));
        return hasMx;
    }

    private readonly record struct CacheEntry(bool HasMx, DateTimeOffset ExpiresAt);
}
