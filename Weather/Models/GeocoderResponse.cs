using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather.Models
{
    public class GeocoderResponse
    {
        public Response Response { get; set; }
    }

    public class Response
    {
        public GeoObjectCollection GeoObjectCollection { get; set; }
    }

    public class GeoObjectCollection
    {
        public MetaDataProperty MetaDataProperty { get; set; }
        public List<FeatureMember> featureMember { get; set; }
    }

    public class MetaDataProperty
    {
        public GeocoderResponseMetaData GeocoderResponseMetaData { get; set; }
    }

    public class GeocoderResponseMetaData
    {
        public string request { get; set; }
        public string found { get; set; }
        public string results { get; set; }
    }

    public class FeatureMember
    {
        public GeoObject GeoObject { get; set; }
    }

    public class GeoObject
    {
        public MetaDataProperty MetaDataProperty { get; set; }
        public string name { get; set; }
        public Point Point { get; set; }
    }

    public class Point
    {
        public string pos { get; set; } 
    }
}
