using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Entities
{
    public class AccountBalance
    {
        [Required]
        public long AccountID { get; set; }
        [Required]
        public int CurrencyType { get; set; }
        public string AccountName { get; set; }
        public long Balance { get; set; }
    }
}
