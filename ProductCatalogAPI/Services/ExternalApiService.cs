using System.Text.Json;

namespace ProductCatalogAPI.Services
{
    public class ExternalApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ExternalApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<CepResponse?> GetAddressByCepAsync(string cep)
        {
            try
            {
                var cepApiUrl = _configuration["ExternalApis:CepApiUrl"];
                var response = await _httpClient.GetAsync($"{cepApiUrl}/{cep}/json/");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CepResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Erro ao consultar CEP: {ex.Message}");
            }
            
            return null;
        }

        public async Task<CurrencyResponse?> GetExchangeRatesAsync(string baseCurrency = "USD")
        {
            try
            {
                var currencyApiUrl = _configuration["ExternalApis:CurrencyApiUrl"];
                var response = await _httpClient.GetAsync($"{currencyApiUrl}/{baseCurrency}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<CurrencyResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Erro ao consultar taxa de câmbio: {ex.Message}");
            }
            
            return null;
        }

        public async Task<List<RandomUserResponse>?> GetRandomUsersAsync(int count = 5)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://randomuser.me/api/?results={count}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<RandomUserApiResponse>(json, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    return result?.Results;
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Erro ao consultar usuários aleatórios: {ex.Message}");
            }
            
            return null;
        }
    }

    public class CepResponse
    {
        public string Cep { get; set; } = string.Empty;
        public string Logradouro { get; set; } = string.Empty;
        public string Complemento { get; set; } = string.Empty;
        public string Bairro { get; set; } = string.Empty;
        public string Localidade { get; set; } = string.Empty;
        public string Uf { get; set; } = string.Empty;
        public string Ibge { get; set; } = string.Empty;
        public string Gia { get; set; } = string.Empty;
        public string Ddd { get; set; } = string.Empty;
        public string Siafi { get; set; } = string.Empty;
        public bool Erro { get; set; }
    }

    public class CurrencyResponse
    {
        public string Base { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    public class RandomUserApiResponse
    {
        public List<RandomUserResponse> Results { get; set; } = new();
    }

    public class RandomUserResponse
    {
        public string Gender { get; set; } = string.Empty;
        public UserName Name { get; set; } = new();
        public UserLocation Location { get; set; } = new();
        public string Email { get; set; } = string.Empty;
        public UserPicture Picture { get; set; } = new();
    }

    public class UserName
    {
        public string Title { get; set; } = string.Empty;
        public string First { get; set; } = string.Empty;
        public string Last { get; set; } = string.Empty;
    }

    public class UserLocation
    {
        public UserStreet Street { get; set; } = new();
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Postcode { get; set; } = string.Empty;
    }

    public class UserStreet
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class UserPicture
    {
        public string Large { get; set; } = string.Empty;
        public string Medium { get; set; } = string.Empty;
        public string Thumbnail { get; set; } = string.Empty;
    }
}