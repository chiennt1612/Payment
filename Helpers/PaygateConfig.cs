using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Helpers
{
    public interface IPaygateConfig
    {
        public string MERCHANTID { get; set; }
        public string SECRETKEY { get; set; }
        public string SHAREKEY { get; set; }
        public string MERCHANTPRIVATEKEY { get; set; }
        public string PAYGATEPUBLICKEY { get; set; }
        public string PAYGATEURL { get; set; }
        public List<string> METHODLIST { get; set; }
        public string DATEFORMAT { get; set; }

        public string URLReturn { get; set; }
        public string IPAddress { get; set; }
    }
    public class PaygateConfig : IPaygateConfig
    {
        public string MERCHANTID { get; set; }
        public string SECRETKEY { get; set; }
        public string SHAREKEY { get; set; }
        public string MERCHANTPRIVATEKEY { get; set; }
        public string PAYGATEPUBLICKEY { get; set; }
        public string PAYGATEURL { get; set; }
        public List<string> METHODLIST { get; set; }
        public string DATEFORMAT { get; set; }

        public string URLReturn { get; set; }
        public string IPAddress { get; set; }
    }
}
