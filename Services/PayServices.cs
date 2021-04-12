using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Payment.Controllers;
using Payment.Dtos;
using Payment.Helpers;
using Payment.Repository;

namespace Payment.Services
{
    public interface IPayServices
    {
        #region Payment by Xu
        Task<ActionResult<AccountBalanceByCurrencyDTO>> BalanceAsync(ClaimsPrincipal User);
        Task<ActionResult<OrderDTO>> PaymentByXuAsync(ClaimsPrincipal User, OrderPayInput orderInput);
        Task<ActionResult<OrderDTO>> AddByXuAsync(ClaimsPrincipal User, OrderAddInput orderInput);
        #endregion

        #region Payment by Paygate
        Task<ActionResult<OrderProcessing>> CreateOrderAsync(ClaimsPrincipal User, OrderPayInput orderInput);
        Task<ActionResult<ProcessingOutput>> ProcessingAsync(ClaimsPrincipal User, ProcessingInput orderInput);
        Task<ActionResult<OrderDTO>> GetOrderResultAsync(ClaimsPrincipal User, long OrderId);
        Task<ActionResult<OrderDTO>> PostBackOrderResultAsync(ClaimsPrincipal User, long amount, string currency_code, string order_info, string pay_date, string service_name, string status, long transaction_no, long txn_ref, string signature);
        #endregion

        #region Get Orders
        Task<ActionResult<List<OrderDTO>>> OrdersAsync(ClaimsPrincipal User, int count = 10, int skip = 0, int CurrencyType = ConstantCode.Xu, int Status = ConstantCode.Success);
        Task<ActionResult<OrderDTO>> OrderByIdAsync(ClaimsPrincipal User, string Id);
        #endregion
    }

    public class PayServices : IPayServices
    {
        private readonly IElasticClient _elasticClient;
        private readonly IPayRepository _db;
        private readonly ILogger<PayRepository> _logger;
        private readonly IDistributedCache _cache;
        private readonly IPaygateConfig _paygateConfig;

        public PayServices(IElasticClient elasticClient, ILogger<PayRepository> _Log, IPayRepository _db, IDistributedCache _cache, IPaygateConfig paygateConfig)
        {
            this._logger = _Log;
            this._db = _db;
            _elasticClient = elasticClient;
            this._cache = _cache;
            this._paygateConfig = paygateConfig;
        }

        #region Payment by Xu
        public async Task<ActionResult<AccountBalanceByCurrencyDTO>> BalanceAsync(ClaimsPrincipal User)
        {
            AccountBalanceByCurrencyDTO a;
            var key = BaseController.GetKey(User, "Balane", ConstantCode.Xu);
            var v = await _cache.GetDataAsync<AccountBalanceByCurrencyDTO>(key);
            if (v == null)
            {
                var o = BaseController.SetObjAccountBalanceByCurrency(User);
                a = await _db.BalanceAsync(o);
                await _cache.SetDataAsync<AccountBalanceByCurrencyDTO>(a, key);
            }
            else
            {
                a = v;
            }
            _logger.LogInformation($"BalanceAsync: => {JsonConvert.SerializeObject(a)}");
            return a;
        }
        public async Task<ActionResult<OrderDTO>> PaymentByXuAsync(ClaimsPrincipal User, OrderPayInput orderInput)
        {
            long _AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            string _Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            InputOrderDTO a = new InputOrderDTO(_AccountId, _Username, ConstantCode.XuSubtract);
            a.Amount = orderInput.Amount;
            a.SubAmount = 0;
            a.Discount = 0;
            a.Tax = 0;
            a.Description = orderInput.Description;
            a.ReferenceID = orderInput.ReferenceID;
            a.ProductID = orderInput.ProductID;
            a.ProductCode = orderInput.ProductCode;
            a.ProductName = orderInput.ProductName;
            return await _db.PaymentByXuAsync(a);
        }
        public async Task<ActionResult<OrderDTO>> AddByXuAsync(ClaimsPrincipal User, OrderAddInput orderInput)
        {
            long _AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            string _Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            InputOrderDTO a = new InputOrderDTO(_AccountId, _Username, ConstantCode.XuPlus);
            a.Amount = orderInput.Amount;
            a.SubAmount = 0;
            a.Discount = 0;
            a.Tax = 0;
            a.Description = orderInput.Description;
            a.ReferenceID = orderInput.ReferenceID;
            a.ProductID = orderInput.EventID;
            a.ProductCode = orderInput.EventCode;
            a.ProductName = orderInput.EventName;
            return await _db.PaymentByXuAsync(a);
        }
        #endregion

        #region Payment by Paygate
        public async Task<ActionResult<OrderProcessing>> CreateOrderAsync(ClaimsPrincipal User, OrderPayInput orderInput)
        {
            long _AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            string _Username = User.FindFirst(UserConst.Username)?.Value ?? "";
            InputOrderDTO a = new InputOrderDTO(_AccountId, _Username, ConstantCode.Paygate);
            a.Amount = orderInput.Amount;
            a.SubAmount = 0;
            a.Discount = 0;
            a.Tax = 0;
            a.Description = orderInput.Description;
            a.ReferenceID = orderInput.ReferenceID;
            a.ProductID = orderInput.ProductID;
            a.ProductCode = orderInput.ProductCode;
            a.ProductName = orderInput.ProductName;
            return await _db.CreateOrderAsync(a);
        }
        public async Task<ActionResult<ProcessingOutput>> ProcessingAsync(ClaimsPrincipal User, ProcessingInput orderInput)
        {
            long _AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            return await _db.ProcessingAsync(orderInput, _AccountId);
        }
        public async Task<ActionResult<OrderDTO>> GetOrderResultAsync(ClaimsPrincipal User, long OrderId)
        {
            long _AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            return await _db.GetOrderResultAsync(OrderId, _AccountId);
        }

        public async Task<ActionResult<OrderDTO>> PostBackOrderResultAsync(ClaimsPrincipal User, long amount, string currency_code, string order_info, string pay_date, string service_name, string status, long transaction_no, long txn_ref, string signature)
        {
            long _AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            return await _db.PostBackOrderResultAsync(_AccountId, amount, currency_code, order_info, pay_date, service_name, status, transaction_no, txn_ref, signature);
        }
        #endregion

        #region Get Orders
        public async Task<ActionResult<List<OrderDTO>>> OrdersAsync(ClaimsPrincipal User, int count = 10, int skip = 0, int CurrencyType = ConstantCode.Xu, int Status = ConstantCode.Success)
        {
            long AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            return await _db.OrdersAsync(AccountId, count, skip, CurrencyType, Status);
        }
        public async Task<ActionResult<OrderDTO>> OrderByIdAsync(ClaimsPrincipal User, string Id)
        {
            long AccountId = long.Parse(User.FindFirst(UserConst.AccountId)?.Value ?? "0");
            return await _db.OrderByIdAsync(AccountId, Id);
        }
        #endregion
    }
}
