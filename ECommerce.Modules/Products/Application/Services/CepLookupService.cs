using System.Text.Json;
using System.Text.Json.Serialization;
using ECommerce.Modules.Products.Application.DTOs;
using ECommerce.Shared.Results;

namespace ECommerce.Modules.Products.Application.Services;

public interface ICepLookupService
{
    Task<Result<CepAddressResponse>> LookupAsync(string zipCode, CancellationToken cancellationToken = default);
}

public sealed class CepLookupService : ICepLookupService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;

    public CepLookupService(HttpClient http)
    {
        _http = http;
    }

    public async Task<Result<CepAddressResponse>> LookupAsync(
        string zipCode,
        CancellationToken cancellationToken = default)
    {
        var digits = new string((zipCode ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
        {
            return Result<CepAddressResponse>.Failure("Informe um CEP com 8 dígitos.");
        }

        var address = await TryViaCepAsync(digits, cancellationToken)
                      ?? await TryBrasilApiAsync(digits, cancellationToken);

        if (address is null)
        {
            return Result<CepAddressResponse>.Failure("CEP não encontrado.", 404);
        }

        return Result<CepAddressResponse>.Success(address);
    }

    private async Task<CepAddressResponse?> TryViaCepAsync(string digits, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync($"https://viacep.com.br/ws/{digits}/json/", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<ViaCepPayload>(json, JsonOptions);
            if (payload is null || payload.HasError || string.IsNullOrWhiteSpace(payload.Localidade))
            {
                return null;
            }

            return new CepAddressResponse
            {
                ZipCode = FormatCep(digits),
                Street = payload.Logradouro?.Trim() ?? string.Empty,
                Neighborhood = payload.Bairro?.Trim() ?? string.Empty,
                City = payload.Localidade.Trim(),
                State = (payload.Uf ?? string.Empty).Trim().ToUpperInvariant()
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private async Task<CepAddressResponse?> TryBrasilApiAsync(string digits, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.GetAsync($"https://brasilapi.com.br/api/cep/v2/{digits}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var payload = JsonSerializer.Deserialize<BrasilApiPayload>(json, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.City))
            {
                return null;
            }

            return new CepAddressResponse
            {
                ZipCode = FormatCep(digits),
                Street = payload.Street?.Trim() ?? string.Empty,
                Neighborhood = payload.Neighborhood?.Trim() ?? string.Empty,
                City = payload.City.Trim(),
                State = (payload.State ?? string.Empty).Trim().ToUpperInvariant()
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return null;
        }
    }

    private static string FormatCep(string digits)
        => $"{digits[..5]}-{digits[5..]}";

    private sealed class ViaCepPayload
    {
        public string? Cep { get; set; }
        public string? Logradouro { get; set; }
        public string? Bairro { get; set; }
        public string? Localidade { get; set; }
        public string? Uf { get; set; }

        [JsonPropertyName("erro")]
        public JsonElement Erro { get; set; }

        public bool HasError =>
            Erro.ValueKind is JsonValueKind.True
            || (Erro.ValueKind is JsonValueKind.String
                && bool.TryParse(Erro.GetString(), out var flag)
                && flag);
    }

    private sealed class BrasilApiPayload
    {
        public string? Cep { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Neighborhood { get; set; }
        public string? Street { get; set; }
    }
}
