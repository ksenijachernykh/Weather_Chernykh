using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather.Models
{
    public class DataResponse
    {
        public List<Forecast> forecasts { get; set; }
    }

    public class Forecast
    {
        public DateTime date { get; set; }
        public List<Hour> hours { get; set; }
    }

    public class Hour
    {
        public string hour { get; set; }
        public string condition { get; set; }
        public int humidity { get; set; }
        public int prec_type { get; set; }
        public int temp { get; set; }

        public string ToCondition()
        {
            string result = "";

            switch (this.condition)
            {
                case "clear":
                    result = "ясно";
                    break;
                case "partly-cloudy":
                    result = "малооблачно";
                    break;
                case "cloudy":
                    result = "облачно с прояснениями";
                    break;
                case "overcast":
                    result = "пасмурно";
                    break;
                case "light-rain":
                    result = "небольшой дождь";
                    break;
                case "rain":
                    result = "дождь";
                    break;
                case "heavy-rain":
                    result = "сильный дождь";
                    break;
                case "showers":
                    result = "ливень";
                    break;
                case "wet-snow":
                    result = "дождь со снегом";
                    break;
                case "light-snow":
                    result = "небольшой снег";
                    break;
                case "snow":
                    result = "снег";
                    break;
                case "snow-showers":
                    result = "снегопад";
                    break;
                case "hail":
                    result = "град";
                    break;
                case "thunderstorm":
                    result = "гроза";
                    break;
                case "thunderstorm-with-rain":
                    result = "дождь с грозой";
                    break;
                case "thunderstorm-with-hail":
                    result = "гроза с градом";
                    break;
            }
            return result;
        }
    }
}
