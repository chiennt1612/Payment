using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using Payment.Dtos;
using Payment.Helpers;

namespace Payment.Controllers
{
    public static class BaseController
    {
        public static string GetKey(ClaimsPrincipal User, string name, int CurrencyType)
        {
            switch (name.ToLower())
            {
                case "info":
                    return (User.FindFirst(UserConst.AccountId)?.Value ?? "0") + "_GetInfo";
                default:
                    return (User.FindFirst(UserConst.AccountId)?.Value ?? "0") + "_GetBalance_" + CurrencyType.ToString();
            }
            
        }
        public static AccountBalanceDTO SetObjAccountBalance(ClaimsPrincipal User)
        {
            var o = new AccountBalanceDTO();
            o.ConfirmId = User.FindFirst(UserConst.ConfirmId)?.Value ?? Guid.NewGuid().ToString(); 
            o.AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            o.Email = User.FindFirst(UserConst.Email)?.Value ?? "";
            o.Fullname = User.FindFirst(UserConst.Name)?.Value ?? User.FindFirst(UserConst.GivenName)?.Value ?? User.FindFirst(UserConst.Surname)?.Value ?? "";
            o.Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            o.Phone = User.FindFirst(UserConst.Phone)?.Value ?? "";
            return o;
        }

        public static AccountBalanceByCurrencyDTO SetObjAccountBalanceByCurrency(ClaimsPrincipal User)
        {
            var o = new AccountBalanceByCurrencyDTO();
            o.ConfirmId = User.FindFirst(UserConst.ConfirmId)?.Value ?? Guid.NewGuid().ToString();
            o.AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            o.Email = User.FindFirst(UserConst.Email)?.Value ?? "";
            o.Fullname = User.FindFirst(UserConst.Name)?.Value ?? User.FindFirst(UserConst.GivenName)?.Value ?? User.FindFirst(UserConst.Surname)?.Value ?? "";
            o.Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            o.Phone = User.FindFirst(UserConst.Phone)?.Value ?? "";
            return o;
        }

        public static InputTransactionDTO SetObjInputTransaction(ClaimsPrincipal User, int CurrencyType)
        {
            
            long AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            string Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            var o = new InputTransactionDTO(AccountId, Username, CurrencyType);
            return o;
        }

        public static OutputTransactionDTO SetObjOutputTransaction(ClaimsPrincipal User, int CurrencyType)
        {

            long AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            string Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            var o = new OutputTransactionDTO(AccountId, Username, CurrencyType);
            return o;
        }
    }
}
