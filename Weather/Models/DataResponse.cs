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
    }
}
