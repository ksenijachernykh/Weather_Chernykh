using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Weather.Models;

namespace Weather.Classes
{
    public class GetWeather
    {
        public static string Url = "https://api.weather.yandex.ru/v2/forecast";
        public static string Key = "demo_yandex_weather_api_key_ca6d09349ba0";

        public static async Task<DataResponse> Get(float lat, float lon)
        {
            try
            {
                DataResponse DataResponse = null;

                string url = $"{Url}?lat={lat}&lon={lon}".Replace(",", ".");
                using (HttpClient Client = new HttpClient())
                {
                    using (HttpRequestMessage Request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        Request.Headers.Add("X-Yandex-Weather-Key", Key);

                        using (var Response = await Client.SendAsync(Request))
                        {
                            if (!Response.IsSuccessStatusCode)
                            {
                                throw new Exception($"Ошибка API: {Response.StatusCode}");
                            }

                            string ContentResponse = await Response.Content.ReadAsStringAsync();
                            DataResponse = JsonConvert.DeserializeObject<DataResponse>(ContentResponse);
                        }
                    }
                }

                return DataResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка получения погоды: {ex.Message}");
            }
        }

        // Новый метод для получения погоды с кэшированием
        public static async Task<DataResponse> GetWeatherData(string cityName, string userId)
        {
            return await WeatherCache.GetWeatherData(cityName, userId);
        }
    }
}