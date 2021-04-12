using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payment.Dtos;
using Payment.ExceptionHandling;
using Payment.Helpers;
using Payment.Repository;
using Payment.Services;

namespace Payment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [TypeFilter(typeof(ControllerExceptionFilterAttribute))]
    [Produces("application/json", "application/problem+json")]
    [Authorize(Roles = "customer")]
    public class PaygateController : Controller
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<PaygateController> _logger;
        private readonly IPayServices _db;
        private readonly IElasticClient _elasticClient;

        public PaygateController(IElasticClient _elasticClient, ILogger<PaygateController> _logger, IPayServices _db, IDistributedCache _cache)
        {
            this._logger = _logger;
            this._db = _db;
            this._elasticClient = _elasticClient;
            this._cache = _cache;
        }

        #region Payment by Xu
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<AccountBalanceByCurrencyDTO>> BalanceXuAsync()
        {
            return await _db.BalanceAsync(User);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<OrderDTO>> PaymentByXuAsync(OrderPayInput orderInput)
        {
            var r = await _db.PaymentByXuAsync(User, orderInput);
            //if (r)
            //{
            var r1 = _cache.RemoveDataAsync(BaseController.GetKey(User, "Info", ConstantCode.Xu));
            var r2 = _cache.RemoveDataAsync(BaseController.GetKey(User, "Balane", ConstantCode.Xu));
            Task.WaitAll(r1, r2);
            //}
            return r;
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<OrderDTO>> AddByXuAsync(OrderAddInput orderInput)
        {
            var r = await _db.AddByXuAsync(User, orderInput);
            //if (r)
            //{
            var r1 = _cache.RemoveDataAsync(BaseController.GetKey(User, "Info", ConstantCode.Xu));
            var r2 = _cache.RemoveDataAsync(BaseController.GetKey(User, "Balane", ConstantCode.Xu));
            Task.WaitAll(r1, r2);
            //}
            return r;
        }
        #endregion

        #region Payment by Paygate
        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<OrderProcessing>> CreateOrderAsync(OrderPayInput orderInput)
        {
            return await _db.CreateOrderAsync(User, orderInput);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<ProcessingOutput>> ProcessingAsync(ProcessingInput orderInput)
        {
            return await _db.ProcessingAsync(User, orderInput);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<OrderDTO>> GetOrderResultAsync(long OrderId)
        {
            return await _db.GetOrderResultAsync(User, OrderId);
        }

        [HttpGet]
        [Route("[action]")]
        [AllowAnonymous]
        public async Task<ActionResult<OrderDTO>> PostBackOrderResultAsync(
            long amount, string currency_code, string order_info, string pay_date, string service_name, string status, long transaction_no, long txn_ref, string signature)
        {
            //amount = 10000
            //currency_code = VND
            //order_info = test + 10 + ngan_thanh + to % C3 % A1n + 10061
            //pay_date = 15 - 03 - 2021T19 % 3a53 % 3a09
            //service_name = Thanh + to % C3 % A1n + qua + Ng % C3 % A2n + h % C3 % A0ng + NCB
            //status = Success
            //transaction_no = 106091
            //txn_ref = 10061
            //signature = R3Y % 2fHjzjqzOF4ENIQgqmfPuU % 2f3DdPrwOd8v4V % 2fM % 2fwiBgg8PNBT27HENC5tXGTfpqJuGeKI4ezEfxoT687ls0x % 2bPmJfwvOe % 2bYQQqmYklDVqb97wCVYSabqKBjOixBsSbZaCrCcK % 2bqPU194wSa69QLD1sXtxZW7pgu229VO % 2f % 2bcXuYAZMAG % 2fyzaRUk % 2fwQjz7TuMyK % 2bpnj % 2fE8AoubxsbNkWunKZt9XW0Xc9a % 2bXVEjVDMWJBF5GBRUMJVyJvylxo0412FKDAHVytXe % 2bkw2i % 2bbvKhFo0tuP1xe69qrBz5ZuZOPZSFXlioEK5iSsu3XTvgmC46LgEXLYbjFUYSu6pJgyxobfw % 3d % 3d
            return await _db.PostBackOrderResultAsync(User, amount, currency_code, order_info, pay_date, service_name, status, transaction_no, txn_ref, signature);
        }
        #endregion

        #region Get Orders
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<OrderDTO>>> OrdersAsync(int count = 10, int skip = 0, int CurrencyType = 1, int Status = 9)
        {
            return await _db.OrdersAsync(User, count, skip, CurrencyType, Status);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<OrderDTO>> OrderByIdAsync(string Id)
        {
            return await _db.OrderByIdAsync(User, Id);
        }
        #endregion
    }
}
