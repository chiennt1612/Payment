using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Dtos
{
    public class PayGateResponse
    {
        public string code { get; set; }
        public string message { get; set; }
        public string signature { get; set; }
        public string data { get; set; }
        public string raw_data { get; set; }
    }

    public class PayGateServices
    {
        public int service_code { get; set; }
        public string service_group { get; set; }
        public string service_name { get; set; }
        public string short_name { get; set; }
        public string thumbnail { get; set; }
    }
}
