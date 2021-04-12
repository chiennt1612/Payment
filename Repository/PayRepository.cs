using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payment.DBContext;
using Payment.Dtos;
using Payment.Entities;
using Payment.Helpers;

namespace Payment.Repository
{
    public static class ConstantCode
    {
        public const int Creator = 1;
        public const int Success = 9;
        public const int Fail = -9;
        public const int Unknown = -99;
        public const int Prosessing = 2;

        public const string CreatorName = "Khởi tạo đơn";
        public const string SuccessName = "Đơn xử lý thành công";
        public const string FailName = "Đơn xử lý thất bại";
        public const string UnknownName = "Không xác định";
        public const string ProsessingName = "Đang xử lý";

        public const int Xu = 1;
        public const int VND = 2;

        public const int XuPlus = 1;
        public const int XuSubtract = 2;
        public const int Paygate = 3;
    }

    public enum CurrencyCode
    {
        Xu = ConstantCode.Xu,
        VND = ConstantCode.VND
    }

    public enum StatusCode
    {
        Creator = ConstantCode.Creator,
        Success = ConstantCode.Success,
        Fail = ConstantCode.Fail,
        Unknown = ConstantCode.Unknown
    }

    public enum ServiceType
    {
        XuPlus = ConstantCode.XuPlus,
        XuSubtract = ConstantCode.XuSubtract,
        Paygate = ConstantCode.Paygate
    }

    public interface IPayRepository
    {
        #region Payment by Xu
        Task<AccountBalanceByCurrencyDTO> BalanceAsync(AccountBalanceByCurrencyDTO balanceDTO);
        Task<OrderDTO> PaymentByXuAsync(InputOrderDTO orderInput);
        #endregion

        #region Payment by Paygate
        Task<OrderProcessing> CreateOrderAsync(InputOrderDTO orderInput);
        Task<ProcessingOutput> ProcessingAsync(ProcessingInput orderInput, long AccountID);
        Task<OrderDTO> GetOrderResultAsync(long OrderID, long AccountID);
        Task<OrderDTO> PostBackOrderResultAsync(long AccountID, long amount, string currency_code, string order_info, string pay_date, string service_name, string status, long transaction_no, long txn_ref, string signature);
        #endregion

        #region Get Orders
        Task<List<OrderDTO>> OrdersAsync(long AccountId, int count, int skip = 0, int CurrencyType = ConstantCode.Xu, int Status = ConstantCode.Success);
        Task<OrderDTO> OrderByIdAsync(long AccountId, string Id);
        #endregion
    }

    public class PayRepository : IPayRepository
    {
        private readonly IElasticClient _elasticClient;
        private readonly AppDbContext _db;
        private readonly ILogger<PayRepository> _Log;
        private readonly IPaygateConfig _paygateConfig;

        public PayRepository(IElasticClient elasticClient, ILogger<PayRepository> Log, AppDbContext db, IPaygateConfig paygateConfig)
        {
            this._Log = Log;
            this._db = db;
            this._elasticClient = elasticClient;
            this._paygateConfig = paygateConfig;
        }

        #region Payment by Xu
        public async Task<AccountBalanceByCurrencyDTO> BalanceAsync(AccountBalanceByCurrencyDTO balanceDTO)
        {
            try
            {
                var in1 = new SqlParameter
                {
                    ParameterName = "@_CurrencyType",
                    DbType = System.Data.DbType.Int32,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = ConstantCode.Xu
                };
                var in2 = new SqlParameter
                {
                    ParameterName = "@_AccountID",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = balanceDTO.AccountId
                };
                var out1 = new SqlParameter
                {
                    ParameterName = "@_Balance",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Output
                };
                _Log.LogInformation($"EXEC SP_Account_GetBalance_ByCurrencyType {ConstantCode.Xu}, {balanceDTO.AccountId}, @_Balance OUTPUT");
                var d = await _db.Database.ExecuteSqlRawAsync("EXEC SP_Account_GetBalance_ByCurrencyType {0}, {1}, {2} OUTPUT", in1, in2, out1);

                balanceDTO.Balance = (long)(out1?.Value ?? 0);
                _Log.LogInformation($"AccountInfo: {JsonConvert.SerializeObject(balanceDTO)}");
                return balanceDTO;
            }
            catch (Exception ex)
            {
                _Log.LogError($"AccountInfo: {JsonConvert.SerializeObject(balanceDTO)}; Error: {ex.Message}");
                return balanceDTO;
            }
        }
        public async Task<OrderDTO> PaymentByXuAsync(InputOrderDTO orderInput)
        {
            #region Create Order
            // Output param
            #region Output Param
            var ResponseStatus = new SqlParameter
            {
                ParameterName = "@_ResponseStatus",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Output
            };
            var Message = new SqlParameter
            {
                ParameterName = "@_Message",
                //DbType = System.Data.DbType.String,
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 300,
                Direction = System.Data.ParameterDirection.Output
            };
            #endregion

            // Input param
            #region Input Param
            var ServiceID = new SqlParameter
            {
                ParameterName = "@_ServiceID",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ServiceID
            };
            var AccountID = new SqlParameter
            {
                ParameterName = "@_AccountID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.AccountID
            };
            var Username = new SqlParameter
            {
                ParameterName = "@_Username",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 150,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Username
            };
            var Amount = new SqlParameter
            {
                ParameterName = "@_Amount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Amount
            };
            var SubAmount = new SqlParameter
            {
                ParameterName = "@_SubAmount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.SubAmount
            };
            var Discount = new SqlParameter
            {
                ParameterName = "@_Discount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Discount
            };
            var Tax = new SqlParameter
            {
                ParameterName = "@_Tax",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Tax
            };
            var Description = new SqlParameter
            {
                ParameterName = "@_Description",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 250,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Description
            };
            var ReferenceID = new SqlParameter
            {
                ParameterName = "@_ReferenceID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ReferenceID
            };
            var ProductID = new SqlParameter
            {
                ParameterName = "@_ProductID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ProductID
            };
            var ProductCode = new SqlParameter
            {
                ParameterName = "@_ProductCode",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 30,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ProductCode
            };
            var ProductName = new SqlParameter
            {
                ParameterName = "@_ProductName",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 200,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ProductName
            };
            #endregion

            // Exec store
            await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12} OUTPUT, {13} OUTPUT",
                    ServiceID, AccountID, Username, Amount, SubAmount, Discount, Tax,
                    Description, ReferenceID, ProductID, ProductCode, ProductName, Message, ResponseStatus);

            long OrderID = long.Parse(ResponseStatus?.Value.ToString() ?? "0");
            _Log.LogInformation(String.Format("EXEC SP_Orders_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12} OUTPUT, {13} OUTPUT",
                    orderInput.ServiceID, orderInput.AccountID, orderInput.Username, orderInput.Amount, orderInput.SubAmount, orderInput.Discount, orderInput.Tax,
                    orderInput.Description, orderInput.ReferenceID, orderInput.ProductID, orderInput.ProductCode, orderInput.ProductName, (Message?.Value??""), OrderID));
            #endregion

            #region Processing
            if (OrderID > 0)
            {
                var a = new OrderDTO()
                {
                    OrderID = OrderID,
                    ServiceID = orderInput.ServiceID,
                    ServiceName = orderInput.ServiceName,
                    ServiceType = ConstantCode.Xu,
                    AccountID = orderInput.AccountID,
                    Username = orderInput.Username,
                    Amount = orderInput.Amount,
                    SubAmount = orderInput.SubAmount,
                    Discount = orderInput.Discount,
                    Tax = orderInput.Tax,
                    Description = orderInput.Description,
                    ReferenceID = orderInput.ReferenceID,
                    ProductID = orderInput.ProductID,
                    ProductCode = orderInput.ProductCode,
                    ProductName = orderInput.ProductName,
                    CreatedTime = DateTime.Now,
                    Status = ConstantCode.Creator,
                    StatusName = ConstantCode.CreatorName,
                    CurrencyType = ConstantCode.Xu
                };
                // Output param
                #region Output Param
                var _ResponseStatus = new SqlParameter
                {
                    ParameterName = "@_ResponseStatus",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Output
                };
                var _Balance = new SqlParameter
                {
                    ParameterName = "@_Balance",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Output
                };
                var _Message = new SqlParameter
                {
                    ParameterName = "@_Message",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 300,
                    Direction = System.Data.ParameterDirection.Output
                };
                #endregion

                // Input param
                #region Input Param
                var _OrderID = new SqlParameter
                {
                    ParameterName = "@_OrderID",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = OrderID
                };
                var _AccountID = new SqlParameter
                {
                    ParameterName = "@_AccountID",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = orderInput.AccountID
                };
                #endregion

                // Exec store
                await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByXu {0}, {1}, {2} OUTPUT, {3} OUTPUT, {4} OUTPUT",
                        _OrderID, _AccountID, _Balance, _Message, _ResponseStatus);

                long TransactionID = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                long Balance = long.Parse(_Balance?.Value.ToString() ?? "0");
                _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByXu {0}, {1}, {2} OUTPUT, {3} OUTPUT, {4} OUTPUT",
                        OrderID, orderInput.AccountID, Balance, (_Message?.Value ?? ""), TransactionID));

                if (TransactionID > 0)
                {
                    a.Status = ConstantCode.Success;
                    a.StatusName = ConstantCode.SuccessName;
                }
                else
                {
                    a.Status = ConstantCode.Fail;
                    a.StatusName = ConstantCode.FailName;
                }
                await OrderAddAsync(a);
                return a;
            }
            #endregion
            return default;
        }
        #endregion

        #region Payment by Paygate
        public async Task<OrderProcessing> CreateOrderAsync(InputOrderDTO orderInput)
        {
            #region Create Order
            // Output param
            #region Output Param
            var ResponseStatus = new SqlParameter
            {
                ParameterName = "@_ResponseStatus",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Output
            };
            var Message = new SqlParameter
            {
                ParameterName = "@_Message",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 300,
                Direction = System.Data.ParameterDirection.Output
            };
            #endregion

            // Input param
            #region Input Param
            var ServiceID = new SqlParameter
            {
                ParameterName = "@_ServiceID",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ServiceID
            };
            var AccountID = new SqlParameter
            {
                ParameterName = "@_AccountID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.AccountID
            };
            var Username = new SqlParameter
            {
                ParameterName = "@_Username",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 150,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Username
            };
            var Amount = new SqlParameter
            {
                ParameterName = "@_Amount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Amount
            };
            var SubAmount = new SqlParameter
            {
                ParameterName = "@_SubAmount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.SubAmount
            };
            var Discount = new SqlParameter
            {
                ParameterName = "@_Discount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Discount
            };
            var Tax = new SqlParameter
            {
                ParameterName = "@_Tax",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Tax
            };
            var Description = new SqlParameter
            {
                ParameterName = "@_Description",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 250,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.Description
            };
            var ReferenceID = new SqlParameter
            {
                ParameterName = "@_ReferenceID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ReferenceID
            };
            var ProductID = new SqlParameter
            {
                ParameterName = "@_ProductID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ProductID
            };
            var ProductCode = new SqlParameter
            {
                ParameterName = "@_ProductCode",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 30,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ProductCode
            };
            var ProductName = new SqlParameter
            {
                ParameterName = "@_ProductName",
                SqlDbType = System.Data.SqlDbType.NVarChar,
                Size = 200,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = orderInput.ProductName
            };
            #endregion

            // Exec store
            await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12} OUTPUT, {13} OUTPUT",
                     ServiceID, AccountID, Username, Amount, SubAmount, Discount, Tax,
                     Description, ReferenceID, ProductID, ProductCode, ProductName, Message, ResponseStatus);

            long OrderID = long.Parse(ResponseStatus?.Value.ToString() ?? "0");
            _Log.LogInformation(String.Format("EXEC SP_Orders_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12} OUTPUT, {13} OUTPUT",
                    ServiceID, AccountID, Username, Amount, SubAmount, Discount, Tax,
                    Description, ReferenceID, ProductID, ProductCode, ProductName, (Message?.Value ?? ""), OrderID));
            #endregion

            #region Get ServiceCode
            // call service paygate lay service_code
            //khởi tạo đối tượng paygate sdk
            IEnumerable<PayGateServices> b = default;
            PayGateSDK client = new PayGateSDK(_paygateConfig.PAYGATEURL, _paygateConfig.MERCHANTID, _paygateConfig.SECRETKEY, _paygateConfig.SHAREKEY, _paygateConfig.MERCHANTPRIVATEKEY, _paygateConfig.PAYGATEPUBLICKEY);
            try
            {
                PayGateResponse servicesReponse = client.GetServices().Result;
                b = JsonConvert.DeserializeObject<IEnumerable<PayGateServices>>(servicesReponse.raw_data);
            }
            catch { }
            
            #endregion

            #region Return Data
            var a = new OrderDTO()
            {
                OrderID = OrderID,
                ServiceID = orderInput.ServiceID,
                ServiceName = orderInput.ServiceName,
                ServiceType = ConstantCode.VND,
                AccountID = orderInput.AccountID,
                Username = orderInput.Username,
                Amount = orderInput.Amount,
                SubAmount = orderInput.SubAmount,
                Discount = orderInput.Discount,
                Tax = orderInput.Tax,
                Description = orderInput.Description,
                ReferenceID = orderInput.ReferenceID,
                ProductID = orderInput.ProductID,
                ProductCode = orderInput.ProductCode,
                ProductName = orderInput.ProductName,
                CreatedTime = DateTime.Now,
                Status = ConstantCode.Creator,
                StatusName = ConstantCode.CreatorName,
                CurrencyType = ConstantCode.VND
            };
            var r = new OrderProcessing()
            {
                Order = a,
                Services = b
            };
            await OrderAddAsync(a);
            #endregion

            return r;
        }
        public async Task<ProcessingOutput> ProcessingAsync(ProcessingInput orderInput, long AccountID)
        {
            // Exec store
            var d = await _db.EntityOrders.FromSqlRaw("EXEC SP_Orders_Get @_OrderID={0}", orderInput.OrderID).ToListAsync();
            if (d.Count > 0)
            {
                var a = new OrderDTO()
                {
                    OrderID = d[0].OrderID,
                    ServiceID = d[0].ServiceID,
                    ServiceName = d[0].ServiceName,
                    ServiceType = ConstantCode.VND,
                    AccountID = d[0].AccountID,
                    Username = d[0].Username,
                    Amount = d[0].Amount,
                    SubAmount = d[0].SubAmount,
                    Discount = d[0].Discount,
                    Tax = d[0].Tax,
                    Description = d[0].Description,
                    ReferenceID = d[0].ReferenceID,
                    ProductID = d[0].ProductID,
                    ProductCode = d[0].ProductCode,
                    ProductName = d[0].ProductName,
                    CreatedTime = d[0].CreatedTime,
                    Status = d[0].Status,
                    StatusName = d[0].StatusName,
                    CurrencyType = ConstantCode.VND
                };
                if (d[0].Status != 1 || d[0].ServiceID != 3 || d[0].AccountID != AccountID)
                {
                    return new ProcessingOutput() { Order = a, Url = ""};
                }
                // khoi tao giao dich tren Paygate
                dynamic a1 = null;
                PayGateSDK client = new PayGateSDK(_paygateConfig.PAYGATEURL, _paygateConfig.MERCHANTID, _paygateConfig.SECRETKEY, _paygateConfig.SHAREKEY, _paygateConfig.MERCHANTPRIVATEKEY, _paygateConfig.PAYGATEPUBLICKEY);
                try
                {
                    PayGateResponse initTransactionReponse = client.InitTransaction(orderInput.ServiceID, orderInput.OrderID.ToString(), a.Description + $"_thanh toán {a.OrderID}", a.Amount.ToString(), a.CreatedTime.ToString(_paygateConfig.DATEFORMAT), _paygateConfig.URLReturn, _paygateConfig.IPAddress).Result;
                    a1 = JObject.Parse(initTransactionReponse.raw_data);
                }
                catch { }

                #region Update Order In Processing
                // Output param
                #region Output Param
                var _ResponseStatus = new SqlParameter
                {
                    ParameterName = "@_ResponseStatus",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Output
                };
                var _Message = new SqlParameter
                {
                    ParameterName = "@_Message",
                    SqlDbType = System.Data.SqlDbType.NVarChar,
                    Size = 300,
                    Direction = System.Data.ParameterDirection.Output
                };
                #endregion

                // Input param
                #region Input Param
                var _OrderID = new SqlParameter
                {
                    ParameterName = "@_OrderID",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = a.OrderID
                };
                var _AccountID = new SqlParameter
                {
                    ParameterName = "@_AccountID",
                    DbType = System.Data.DbType.Int64,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = a.AccountID
                };
                int _StatusVal = 2;
                var _Status = new SqlParameter
                {
                    ParameterName = "@_Status",
                    DbType = System.Data.DbType.Int32,
                    //SqlDbType = System.Data.SqlDbType.Int,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = _StatusVal
                };
                #endregion

                // Exec store
                await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                        _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                long ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                //string Message = (_Message?.Value.ToString() ?? "");
                _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                        a.OrderID, a.AccountID, 2, (_Message?.Value ?? ""), ResponseStatus));
                #endregion

                a.Status = ConstantCode.Prosessing;
                a.StatusName = ConstantCode.ProsessingName;
                await OrderUpdateAsync(a);

                if (a1 == null)
                    return new ProcessingOutput() { Order = a, Url = "Không kết nối được tới Paygate" };
                else
                    return new ProcessingOutput() { Order = a, Url = a1.url };
            }
            
            return default;
        }
        public async Task<OrderDTO> GetOrderResultAsync(long OrderID, long AccountID)
        {
            // Exec store
            var d = await _db.Set<EntityOrder>().FromSqlRaw("EXEC SP_Orders_Get @_OrderID={0}", OrderID).ToListAsync();
            if (d.Count > 0)
            {
                var a = new OrderDTO()
                {
                    OrderID = d[0].OrderID,
                    ServiceID = d[0].ServiceID,
                    ServiceName = d[0].ServiceName,
                    ServiceType = ConstantCode.VND,
                    AccountID = d[0].AccountID,
                    Username = d[0].Username,
                    Amount = d[0].Amount,
                    SubAmount = d[0].SubAmount,
                    Discount = d[0].Discount,
                    Tax = d[0].Tax,
                    Description = d[0].Description,
                    ReferenceID = d[0].ReferenceID,
                    ProductID = d[0].ProductID,
                    ProductCode = d[0].ProductCode,
                    ProductName = d[0].ProductName,
                    CreatedTime = d[0].CreatedTime,
                    Status = d[0].Status,
                    StatusName = d[0].StatusName,
                    CurrencyType = ConstantCode.VND
                };
                if ((d[0].Status != 1 && d[0].Status != 2) || d[0].ServiceID != 3 || d[0].AccountID != AccountID)
                {
                    return a;
                }
                // khoi tao giao dich tren Paygate
                PayGateSDK client = new PayGateSDK(_paygateConfig.PAYGATEURL, _paygateConfig.MERCHANTID, _paygateConfig.SECRETKEY, _paygateConfig.SHAREKEY, _paygateConfig.MERCHANTPRIVATEKEY, _paygateConfig.PAYGATEPUBLICKEY);
                string status = "Provider_Processing";
                int cntCallStatus = 0;
                while (status != "Fail" && status != "Cancel" && status != "Success" && cntCallStatus < 100)
                {
                    dynamic a1 = null;
                    try
                    {
                        PayGateResponse transactionReponse = client.GetTransaction(a.OrderID.ToString(), a.CreatedTime.ToString(_paygateConfig.DATEFORMAT)).Result;
                        a1 = JObject.Parse(transactionReponse.raw_data);
                    }
                    catch { }

                    if (a1 == null)
                        status = "Fail";
                    else
                        status = a1.status;
                    if (status != "Fail" && status != "Cancel" && status != "Success")
                    {
                       await Task.Delay(2000); // 1 giay sau call tiep
                        cntCallStatus++;
                    }
                }
                
                switch (status)
                {
                    case "Success":
                        a.Status = ConstantCode.Success;
                        a.StatusName = ConstantCode.SuccessName;

                        #region Update Order In Processing
                        // Output param
                        #region Output Param
                        var _ResponseStatus = new SqlParameter
                        {
                            ParameterName = "@_ResponseStatus",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        var _Message = new SqlParameter
                        {
                            ParameterName = "@_Message",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 300,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        #endregion

                        // Input param
                        #region Input Param
                        var _OrderID = new SqlParameter
                        {
                            ParameterName = "@_ServiceID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.OrderID
                        };
                        var _AccountID = new SqlParameter
                        {
                            ParameterName = "@_AccountID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.AccountID
                        };
                        int _StatusVal = 9;
                        var _Status = new SqlParameter
                        {
                            ParameterName = "@_Status",
                            DbType = System.Data.DbType.Int32,
                            //SqlDbType = System.Data.SqlDbType.Int,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = _StatusVal
                        };
                        #endregion

                        // Exec store
                        await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                         _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                        long ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                        //string Message = (_Message?.Value.ToString() ?? "");
                        _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                                a.OrderID, a.AccountID, _StatusVal, (_Message?.Value ?? ""), ResponseStatus));

                        await OrderUpdateAsync(a);
                        #endregion
                        break;
                    case "Cancel":
                    case "Fail":
                        a.Status = ConstantCode.Fail;
                        a.StatusName = ConstantCode.FailName;

                        #region Update Order In Processing
                        // Output param
                        #region Output Param
                        _ResponseStatus = new SqlParameter
                        {
                            ParameterName = "@_ResponseStatus",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        _Message = new SqlParameter
                        {
                            ParameterName = "@_Message",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 300,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        #endregion

                        // Input param
                        #region Input Param
                        _OrderID = new SqlParameter
                        {
                            ParameterName = "@_ServiceID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.OrderID
                        };
                        _AccountID = new SqlParameter
                        {
                            ParameterName = "@_AccountID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.AccountID
                        };
                        _StatusVal = -9;
                        _Status = new SqlParameter
                        {
                            ParameterName = "@_Status",
                            DbType = System.Data.DbType.Int32,
                            //SqlDbType = System.Data.SqlDbType.Int,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = _StatusVal
                        };
                        #endregion

                        // Exec store
                        await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                         _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                        ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                        //Message = (_Message?.Value.ToString() ?? "");
                        _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                                a.OrderID, a.AccountID, _StatusVal, (_Message?.Value ?? ""), ResponseStatus));
                        
                        await OrderUpdateAsync(a);
                        #endregion
                        break;
                    default:
                        a.Status = ConstantCode.Unknown;
                        a.StatusName = ConstantCode.UnknownName;

                        #region Update Order In Processing
                        // Output param
                        #region Output Param
                        _ResponseStatus = new SqlParameter
                        {
                            ParameterName = "@_ResponseStatus",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        _Message = new SqlParameter
                        {
                            ParameterName = "@_Message",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 300,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        #endregion

                        // Input param
                        #region Input Param
                        _OrderID = new SqlParameter
                        {
                            ParameterName = "@_ServiceID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.OrderID
                        };
                        _AccountID = new SqlParameter
                        {
                            ParameterName = "@_AccountID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.AccountID
                        };
                        _StatusVal = -99;
                        _Status = new SqlParameter
                        {
                            ParameterName = "@_Status",
                            DbType = System.Data.DbType.Int32,
                            //SqlDbType = System.Data.SqlDbType.Int,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = _StatusVal
                        };
                        #endregion

                        // Exec store
                        await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                         _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                        ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                       // Message = (_Message?.Value.ToString() ?? "");
                        _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                                a.OrderID, a.AccountID, _StatusVal, (_Message?.Value ?? ""), ResponseStatus));
                        
                        await OrderUpdateAsync(a);
                        #endregion
                        break;
                }
                return a;
            }

            return default;
        }
        public async Task<OrderDTO> PostBackOrderResultAsync(long AccountID, long amount, string currency_code, string order_info, string pay_date, string service_name, string status, long transaction_no, long txn_ref, string signature)
        {
            // Exec store
            var d = await _db.Set<EntityOrder>().FromSqlRaw("EXEC SP_Orders_Get @_OrderID={0}", txn_ref).ToListAsync();
            if (d.Count > 0)
            {
                var a = new OrderDTO()
                {
                    OrderID = d[0].OrderID,
                    ServiceID = d[0].ServiceID,
                    ServiceName = d[0].ServiceName,
                    ServiceType = ConstantCode.VND,
                    AccountID = d[0].AccountID,
                    Username = d[0].Username,
                    Amount = d[0].Amount,
                    SubAmount = d[0].SubAmount,
                    Discount = d[0].Discount,
                    Tax = d[0].Tax,
                    Description = d[0].Description,
                    ReferenceID = d[0].ReferenceID,
                    ProductID = d[0].ProductID,
                    ProductCode = d[0].ProductCode,
                    ProductName = d[0].ProductName,
                    CreatedTime = d[0].CreatedTime,
                    Status = d[0].Status,
                    StatusName = d[0].StatusName,
                    CurrencyType = ConstantCode.VND
                };
                if ((d[0].Status != 1 && d[0].Status != 2) || d[0].ServiceID != 3)// || d[0].AccountID != AccountID)
                {
                    return a;
                }

                //PayGateSDK client = new PayGateSDK(_paygateConfig.PAYGATEURL, _paygateConfig.MERCHANTID, _paygateConfig.SECRETKEY, _paygateConfig.SHAREKEY, _paygateConfig.MERCHANTPRIVATEKEY, _paygateConfig.PAYGATEPUBLICKEY);
                //if (!client.VerifySignature(amount.ToString(), currency_code, order_info, pay_date, service_name, status, transaction_no.ToString(), txn_ref.ToString(), signature))
                //{
                //    return a;
                //}

                switch (status)
                {
                    case "Success":
                        a.Status = ConstantCode.Success;
                        a.StatusName = ConstantCode.SuccessName;

                        #region Update Order In Processing
                        // Output param
                        #region Output Param
                        var _ResponseStatus = new SqlParameter
                        {
                            ParameterName = "@_ResponseStatus",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        var _Message = new SqlParameter
                        {
                            ParameterName = "@_Message",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 300,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        #endregion

                        // Input param
                        #region Input Param
                        var _OrderID = new SqlParameter
                        {
                            ParameterName = "@_ServiceID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.OrderID
                        };
                        var _AccountID = new SqlParameter
                        {
                            ParameterName = "@_AccountID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.AccountID
                        };
                        int _StatusVal = 9;
                        var _Status = new SqlParameter
                        {
                            ParameterName = "@_Status",
                            DbType = System.Data.DbType.Int32,
                            //SqlDbType = System.Data.SqlDbType.Int,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = _StatusVal
                        };
                        #endregion

                        // Exec store
                        await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                         _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                        long ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                        //string Message = (_Message?.Value.ToString() ?? "");
                        _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                                a.OrderID, a.AccountID, _StatusVal, (_Message?.Value ?? ""), ResponseStatus));

                        await OrderUpdateAsync(a);
                        #endregion
                        break;
                    case "Cancel":
                    case "Fail":
                        a.Status = ConstantCode.Fail;
                        a.StatusName = ConstantCode.FailName;

                        #region Update Order In Processing
                        // Output param
                        #region Output Param
                        _ResponseStatus = new SqlParameter
                        {
                            ParameterName = "@_ResponseStatus",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        _Message = new SqlParameter
                        {
                            ParameterName = "@_Message",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 300,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        #endregion

                        // Input param
                        #region Input Param
                        _OrderID = new SqlParameter
                        {
                            ParameterName = "@_ServiceID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.OrderID
                        };
                        _AccountID = new SqlParameter
                        {
                            ParameterName = "@_AccountID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.AccountID
                        };
                        _StatusVal = -9;
                        _Status = new SqlParameter
                        {
                            ParameterName = "@_Status",
                            DbType = System.Data.DbType.Int32,
                            //SqlDbType = System.Data.SqlDbType.Int,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = _StatusVal
                        };
                        #endregion

                        // Exec store
                        await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                         _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                        ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                        //Message = (_Message?.Value.ToString() ?? "");
                        _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                                a.OrderID, a.AccountID, _StatusVal, (_Message?.Value ?? ""), ResponseStatus));

                        await OrderUpdateAsync(a);
                        #endregion
                        break;
                    default:
                        a.Status = ConstantCode.Unknown;
                        a.StatusName = ConstantCode.UnknownName;

                        #region Update Order In Processing
                        // Output param
                        #region Output Param
                        _ResponseStatus = new SqlParameter
                        {
                            ParameterName = "@_ResponseStatus",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        _Message = new SqlParameter
                        {
                            ParameterName = "@_Message",
                            SqlDbType = System.Data.SqlDbType.NVarChar,
                            Size = 300,
                            Direction = System.Data.ParameterDirection.Output
                        };
                        #endregion

                        // Input param
                        #region Input Param
                        _OrderID = new SqlParameter
                        {
                            ParameterName = "@_ServiceID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.OrderID
                        };
                        _AccountID = new SqlParameter
                        {
                            ParameterName = "@_AccountID",
                            DbType = System.Data.DbType.Int64,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = a.AccountID
                        };
                        _StatusVal = -99;
                        _Status = new SqlParameter
                        {
                            ParameterName = "@_Status",
                            DbType = System.Data.DbType.Int32,
                            //SqlDbType = System.Data.SqlDbType.Int,
                            Direction = System.Data.ParameterDirection.Input,
                            SqlValue = _StatusVal
                        };
                        #endregion

                        // Exec store
                        await _db.Database.ExecuteSqlRawAsync("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                         _OrderID, _AccountID, _Status, _Message, _ResponseStatus);

                        ResponseStatus = long.Parse(_ResponseStatus?.Value.ToString() ?? "0");
                        // Message = (_Message?.Value.ToString() ?? "");
                        _Log.LogInformation(String.Format("EXEC SP_Orders_UpdateByPaygate {0}, {1}, {2}, {3} OUTPUT, {4} OUTPUT",
                                a.OrderID, a.AccountID, _StatusVal, (_Message?.Value ?? ""), ResponseStatus));

                        await OrderUpdateAsync(a);
                        #endregion
                        break;
                }
                return a;
            }

            return default;
        }
        #endregion

        #region Get Orders
        public async Task<List<OrderDTO>> OrdersAsync(long AccountId, int count, int skip = 0, int CurrencyType = ConstantCode.Xu, int Status = ConstantCode.Success)
        {
            var products = (await GetOrders(AccountId, CurrencyType, Status))
                .OrderByDescending(p => p.CreatedTime)
                .Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
                .Skip(skip)
                .Take(count).ToList()
                ;
            _Log.LogInformation($"List transaction {CurrencyType}, {AccountId}, {count}, {skip}, {products.Count()} OUTPUT");//{JsonConvert.SerializeObject(products)}
            return products;
        }
        public async Task<OrderDTO> OrderByIdAsync(long AccountId, string Id)
        {
            var product = (await GetOrders(AccountId)).FirstOrDefault(p => p.Id == Id);
            _Log.LogInformation($"TransactionByIdAsync {AccountId}, {Id}, {JsonConvert.SerializeObject(product)} OUTPUT");
            return product;
        }
        #endregion

        #region ElasticSearch
        private async Task<IEnumerable<OrderDTO>> GetOrders(long AccountId, int _CurrencyType, int Status)
        {
            var products = (await _elasticClient.SearchAsync<OrderDTO>(a => a
                       .Index("orders")
                       .Query(q => q
                            .Bool(b => b
                                .Must(
                                    bs => bs.Term(p => p.AccountID, AccountId),
                                    bs => bs.Term(p => p.CurrencyType, _CurrencyType),
                                    bs => bs.Term(p => p.Status, Status),
                                    bs => bs.DateRange(p => p.Field("createdTime").GreaterThanOrEquals(DateTime.UtcNow.AddDays(-90)))
                                )
                            )
                           )
                       ))
                .Documents
                //.Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
                ;
            _Log.LogInformation($"GetOrders {_CurrencyType}, {AccountId}, {products.Count()} OUTPUT");//{JsonConvert.SerializeObject(products)}
            return products;
        }

        private async Task<IEnumerable<OrderDTO>> GetOrders(long AccountId, int _CurrencyType)
        {
            var products = (await _elasticClient.SearchAsync<OrderDTO>(a => a
                       .Index("orders")
                       .Query(q => q
                            .Bool(b => b
                                .Must(
                                    bs => bs.Term(p => p.AccountID, AccountId),
                                    bs => bs.Term(p => p.CurrencyType, _CurrencyType),
                                    bs => bs.DateRange(p => p.Field("createdTime").GreaterThanOrEquals(DateTime.UtcNow.AddDays(-90)))
                                )
                            )
                           )
                       ))
                .Documents
                //.Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
                ;
            _Log.LogInformation($"GetOrders {_CurrencyType}, {AccountId}, {products.Count()} OUTPUT");//{JsonConvert.SerializeObject(products)}
            return products;
        }

        private async Task<IEnumerable<OrderDTO>> GetOrders(long AccountId)
        {
            var products = (await _elasticClient.SearchAsync<OrderDTO>(a => a
                       .Index("orders")
                       .Query(q => q
                            .Bool(b => b
                                .Must(
                                    bs => bs.Term(p => p.AccountID, AccountId),
                                    bs => bs.DateRange(p => p.Field("createdTime").GreaterThanOrEquals(DateTime.UtcNow.AddDays(-90)))
                                )
                            )
                           )
                       ))
                .Documents
                //.Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
                ;
            _Log.LogInformation($"GetOrders {AccountId}, {products.Count()} OUTPUT");//{JsonConvert.SerializeObject(products)}
            return products;
        }

        private async Task<bool> OrderUpdateAsync(OrderDTO order)
        {
            _Log.LogInformation($"Starting Update order {JsonConvert.SerializeObject(order)}");
            var product = (await GetOrders(order.AccountID)).FirstOrDefault(p => p.OrderID == order.OrderID);
            if (product == default)
            {
                return false;
            }
            product.Status = order.Status;
            product.StatusName = order.StatusName;
            var index = await _elasticClient.UpdateAsync<OrderDTO>(product, u => u.Doc(product));
            _Log.LogInformation($"Update order {JsonConvert.SerializeObject(product)}, {index.IsValid} OUTPUT");
            if (index.IsValid)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private async Task<bool> OrderAddAsync(OrderDTO product)
        {
            ////var a = Newtonsoft.Json.Linq.JObject.Parse(JsonConvert.SerializeObject(product));
            //if ((await GetOrders(product.AccountID, product.CurrencyType)).Any(p => p.Id == product.Id))
            //{
            //    var index = await _elasticClient.UpdateAsync<OrderDTO>(product, u => u.Doc(product));
            //    _Log.LogInformation($"Update order {product.AccountID}, {JsonConvert.SerializeObject(product)}, {index.IsValid} OUTPUT");
            //    if (index.IsValid)
            //    {
            //        return true;
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}
            //else
            //{
                var index = await _elasticClient.IndexDocumentAsync<OrderDTO>(product);
                _Log.LogInformation($"Insert order {JsonConvert.SerializeObject(product)}, {index.IsValid} OUTPUT");
                if (index.IsValid)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            //}
        }
        
        #endregion
    }
}
