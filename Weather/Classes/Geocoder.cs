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
    public class Geocoder
    {
        private static readonly string ApiKey = "e1562ea2-8f0a-4cee-b73e-bc04405fa1d7";
        private static readonly string BaseUrl = "https://geocode-maps.yandex.ru/1.x/";

        public static async Task<(float lat, float lon)> GetCoordinates(string address)
        {
            string url = $"{BaseUrl}?apikey={ApiKey}&geocode={Uri.EscapeDataString(address)}&format=json";

            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetStringAsync(url);
                var geocoderResponse = JsonConvert.DeserializeObject<GeocoderResponse>(response);

                if (geocoderResponse?.Response?.GeoObjectCollection?.featureMember?.Count > 0)
                {
                    var pos = geocoderResponse.Response.GeoObjectCollection.featureMember[0]
                        .GeoObject.Point.pos;

                    var coordinates = pos.Split(' ');
                    if (coordinates.Length == 2)
                    {
                        float lon = float.Parse(coordinates[0], System.Globalization.CultureInfo.InvariantCulture);
                        float lat = float.Parse(coordinates[1], System.Globalization.CultureInfo.InvariantCulture);
                        return (lat, lon);
                    }
                }
            }
            throw new Exception("Не удалось получить координаты для указанного адреса");
        }
    }
}
