using Microsoft.AspNetCore.Mvc;
using ProductCatalogAPI.Services;

namespace ProductCatalogAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExternalApiController : ControllerBase
    {
        private readonly ExternalApiService _externalApiService;

        public ExternalApiController(ExternalApiService externalApiService)
        {
            _externalApiService = externalApiService;
        }

        /// <summary>
        /// Consulta endereço por CEP usando a API ViaCEP
        /// </summary>
        [HttpGet("cep/{cep}")]
        public async Task<ActionResult<CepResponse>> GetAddressByCep(string cep)
        {
            // Validar formato do CEP
            if (string.IsNullOrEmpty(cep) || cep.Length != 8 || !cep.All(char.IsDigit))
            {
                return BadRequest("CEP deve conter exatamente 8 dígitos numéricos.");
            }

            var result = await _externalApiService.GetAddressByCepAsync(cep);
            
            if (result == null)
            {
                return NotFound("CEP não encontrado ou erro na consulta.");
            }

            if (result.Erro)
            {
                return NotFound("CEP não encontrado.");
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtém taxas de câmbio atuais
        /// </summary>
        [HttpGet("exchange-rates")]
        public async Task<ActionResult<CurrencyResponse>> GetExchangeRates([FromQuery] string baseCurrency = "USD")
        {
            var validCurrencies = new[] { "USD", "EUR", "BRL", "GBP", "JPY", "CAD", "AUD" };
            
            if (!validCurrencies.Contains(baseCurrency.ToUpper()))
            {
                return BadRequest($"Moeda base inválida. Moedas válidas: {string.Join(", ", validCurrencies)}");
            }

            var result = await _externalApiService.GetExchangeRatesAsync(baseCurrency.ToUpper());
            
            if (result == null)
            {
                return NotFound("Erro ao consultar taxas de câmbio.");
            }

            return Ok(result);
        }

        /// <summary>
        /// Converte valor entre moedas
        /// </summary>
        [HttpGet("convert")]
        public async Task<ActionResult<object>> ConvertCurrency(
            [FromQuery] decimal amount,
            [FromQuery] string from = "USD",
            [FromQuery] string to = "BRL")
        {
            if (amount <= 0)
            {
                return BadRequest("O valor deve ser maior que zero.");
            }

            var rates = await _externalApiService.GetExchangeRatesAsync(from.ToUpper());
            
            if (rates == null || !rates.Rates.ContainsKey(to.ToUpper()))
            {
                return NotFound("Erro ao obter taxa de câmbio ou moeda não suportada.");
            }

            var convertedAmount = amount * rates.Rates[to.ToUpper()];

            var result = new
            {
                OriginalAmount = amount,
                FromCurrency = from.ToUpper(),
                ToCurrency = to.ToUpper(),
                ExchangeRate = rates.Rates[to.ToUpper()],
                ConvertedAmount = Math.Round(convertedAmount, 2),
                Date = rates.Date
            };

            return Ok(result);
        }

        /// <summary>
        /// Obtém usuários aleatórios para testes
        /// </summary>
        [HttpGet("random-users")]
        public async Task<ActionResult<List<RandomUserResponse>>> GetRandomUsers([FromQuery] int count = 5)
        {
            if (count <= 0 || count > 50)
            {
                return BadRequest("O número de usuários deve estar entre 1 e 50.");
            }

            var result = await _externalApiService.GetRandomUsersAsync(count);
            
            if (result == null)
            {
                return NotFound("Erro ao consultar usuários aleatórios.");
            }

            return Ok(result);
        }

        /// <summary>
        /// Obtém informações de geolocalização por IP
        /// </summary>
        [HttpGet("geolocation")]
        public async Task<ActionResult<object>> GetGeolocation([FromQuery] string? ip = null)
        {
            try
            {
                // Se não fornecer IP, usa o IP do cliente
                if (string.IsNullOrEmpty(ip))
                {
                    ip = HttpContext.Connection.RemoteIpAddress?.ToString();
                    if (ip == "::1" || ip == "127.0.0.1")
                    {
                        ip = "8.8.8.8"; // IP público para teste local
                    }
                }

                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"http://ip-api.com/json/{ip}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Ok(json);
                }
                
                return NotFound("Erro ao consultar geolocalização.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém cotação atual do Bitcoin
        /// </summary>
        [HttpGet("bitcoin-price")]
        public async Task<ActionResult<object>> GetBitcoinPrice()
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync("https://api.coindesk.com/v1/bpi/currentprice/BRL.json");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Ok(json);
                }
                
                return NotFound("Erro ao consultar cotação do Bitcoin.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtém informações sobre um país
        /// </summary>
        [HttpGet("country/{countryName}")]
        public async Task<ActionResult<object>> GetCountryInfo(string countryName)
        {
            try
            {
                var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"https://restcountries.com/v3.1/name/{countryName}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return Ok(json);
                }
                
                return NotFound("País não encontrado.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erro: {ex.Message}");
            }
        }
    }
}