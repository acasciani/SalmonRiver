using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SalmonRiver.Models
{
    public class SquareLocations
    {
        public List<SquareLocation> locations { get; set; }
    }
    public class SquareLocation
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class SquareErrors
    {
        public List<SquareError> errors { get; set; }
    }

    public class SquareError
    {
        public string category { get; set; }
        public string code { get; set; }
        public string detail { get; set; }
    }
}