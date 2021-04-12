using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Helpers
{
    public class Constants
    {
        public const string Authority = "https://vtalent-id.schoolbus.cf:8443/"; // "https://localhost:5004"; //
        public const string AuthorityMtls = "https://identityserver.local";

        public const string SampleApi = "https://localhost:5005/";
        public const string SampleApiMtls = "https://api.identityserver.local/";
    }

    public class UserConst
    {
        public const string ConfirmId = "AccountId";
        public const string AccountId = "UserId";

        public const string Email = "Username";
        public const string Username = "Username";
        public const string Name = "Name";
        public const string GivenName = "GivenName";
        public const string Surname = "Surname";
        public const string Phone = "Phone";
    }
}
