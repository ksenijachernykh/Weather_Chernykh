using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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
            DataResponse DataResponse = null;

            string url = $"{Url}?lat={lat}&lon={lon}".Replace(",", ".");
            using (HttpClient Client = new HttpClient())
            {
                using (HttpRequestMessage Request = new HttpRequestMessage(
                    HttpMethod.Get,
                    url))
                {
                    Request.Headers.Add("X-Yandex-Weather-Key", Key);
                    using(var Response = await Client.SendAsync(Request))
                    {
                        string ContentResponse = await Response.Content.ReadAsStringAsync();

                        DataResponse = JsonConvert.DeserializeObject<DataResponse>(ContentResponse);
                    }
                }
            }
            return DataResponse;
        }

    }
}
