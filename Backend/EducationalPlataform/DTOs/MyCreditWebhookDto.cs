using System.Text.Json.Serialization;

namespace EducationalPlataform.DTOs;

public sealed class MyCreditWebhookDto
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("tipo")]
    public string? Tipo { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTimeOffset? Timestamp { get; set; }

    [JsonPropertyName("dados")]
    public MyCreditWebhookDadosDto? Dados { get; set; }
}

public sealed class MyCreditWebhookDadosDto
{
    [JsonPropertyName("empresa")]
    public MyCreditWebhookEmpresaDto? Empresa { get; set; }

    [JsonPropertyName("pagamento")]
    public MyCreditWebhookPagamentoDto? Pagamento { get; set; }
}

public sealed class MyCreditWebhookEmpresaDto
{
    [JsonPropertyName("cnpj")]
    public string? Cnpj { get; set; }

    [JsonPropertyName("nome")]
    public string? Nome { get; set; }
}

public sealed class MyCreditWebhookPagamentoDto
{
    [JsonPropertyName("idFaturaPag")]
    public string? IdFaturaPag { get; set; }

    [JsonPropertyName("txid")]
    public string? TxId { get; set; }

    [JsonPropertyName("endToEndId")]
    public string? EndToEndId { get; set; }

    [JsonPropertyName("valor")]
    public decimal? Valor { get; set; }
}
