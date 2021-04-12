using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Models
{
    public class JwtOptionsModel
    {
        public string SecretKey { get; set; }
        public int ExpiryMinutes { get; set; }
        public int ExpiryRefreshToken { get; set; }
        public string Issuer { get; set; }

    }
}
