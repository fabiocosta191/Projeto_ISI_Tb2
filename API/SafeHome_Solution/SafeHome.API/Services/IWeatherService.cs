namespace SafeHome.API.Services
{
    public class WeatherDto
    {
        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public string Description { get; set; }
    }

    public interface IWeatherService
    {
        Task<WeatherDto> GetCurrentWeather(double latitude, double longitude);
    }
}