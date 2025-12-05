using System.Text.Json;
using Microsoft.Extensions.Configuration; // Garante que tens este using

namespace SafeHome.API.Services
{
    public class OpenWeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public OpenWeatherService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenWeather:ApiKey"];
        }

        public async Task<WeatherDto> GetCurrentWeather(double latitude, double longitude)
        {
            // Debug: Verifica se a chave está a chegar vazia
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new WeatherDto { Description = "Erro: A Chave API está vazia ou nula." };
            }

            try
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?lat={latitude}&lon={longitude}&appid={_apiKey}&units=metric&lang=pt";

                var response = await _httpClient.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync(); // Ler a resposta mesmo que seja erro

                if (response.IsSuccessStatusCode)
                {
                    var doc = JsonDocument.Parse(responseBody);
                    return new WeatherDto
                    {
                        Temperature = doc.RootElement.GetProperty("main").GetProperty("temp").GetDouble(),
                        Humidity = doc.RootElement.GetProperty("main").GetProperty("humidity").GetDouble(),
                        Description = doc.RootElement.GetProperty("weather")[0].GetProperty("description").GetString()
                    };
                }
                else
                {
                    // AQUI ESTÁ O SEGREDO: Vamos devolver o erro que a API mandou
                    return new WeatherDto { Description = $"Erro API ({response.StatusCode}): {responseBody}" };
                }
            }
            catch (Exception ex)
            {
                // Devolve o erro do código (ex: erro de JSON)
                return new WeatherDto { Description = $"Erro Crítico: {ex.Message}" };
            }
        }
    }
}