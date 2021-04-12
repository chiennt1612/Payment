using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payment.Entities;

namespace Payment.Dtos
{
    public class AccountBalanceDTO
    {
        public string ConfirmId { get; set; }
        public long AccountId { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public List<BalanceDetail> Balance { get; set; }
    }

    public class AccountBalanceByCurrencyDTO
    {
        public string ConfirmId { get; set; }
        public long AccountId { get; set; }
        public string Username { get; set; }
        public string Fullname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public long Balance { get; set; }
    }

    public class BalanceDetail
    {
        public int CurrencyType { get; set; }
        public long Balance { get; set; }
    }
}
