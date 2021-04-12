using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Payment.DBContext;
using Payment.Dtos;
using Payment.Entities;
using Payment.Helpers;

namespace Payment.Services
{
    public interface IXuServices
    {
        Task<AccountBalanceDTO> GetInfoAsync(AccountBalanceDTO balanceDTO);
        Task<AccountBalanceByCurrencyDTO> BalanceAsync(AccountBalanceByCurrencyDTO balanceDTO);
        Task<IEnumerable<TransactionDTO>> TransactionAsync(long AccountId, int count, int skip = 0);
        Task<TransactionDTO> TransactionByIdAsync(long AccountId, string Id);
        Task<TransactionDTO> AddAsync(InputTransactionDTO input);
        Task<TransactionDTO> DeductAsync(OutputTransactionDTO output);
    }

    public class XuServices : IXuServices
    {
        //private List<TransactionDTO> _cache = new List<TransactionDTO>();
        private readonly IElasticClient _elasticClient;
        private readonly AppDbContext _db;
        private readonly ILogger<XuServices> _Log;
        private const int CurrencyType = 2;
        public XuServices(IElasticClient elasticClient, ILogger<XuServices> _Log, AppDbContext _db)
        {
            this._Log = _Log;
            this._db = _db;
            _elasticClient = elasticClient;
            //var searchRequest = new SearchRequest<TransactionDTO>();
            //_cache =_elasticClient.Search<TransactionDTO>(searchRequest).Documents.Where<TransactionDTO>(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90)).ToList();
        }

        public async Task<AccountBalanceDTO> GetInfoAsync(AccountBalanceDTO balanceDTO)
        {
            try
            {
                _Log.LogInformation($"EXEC SP_Account_GetAccountInfo @_AccountID={balanceDTO.AccountId}");
                var d = await _db.Set<AccountBalance>().FromSqlRaw("EXEC SP_Account_GetAccountInfo @_AccountID={0}", balanceDTO.AccountId).ToListAsync();
                var l = (from a in d select new BalanceDetail() { Balance = a.Balance, CurrencyType = a.CurrencyType }).ToList();
                balanceDTO.Balance = l;
                _Log.LogInformation($"AccountInfo: {JsonConvert.SerializeObject(balanceDTO)}");
                return balanceDTO;
            }
            catch (Exception ex)
            {
                _Log.LogError($"AccountInfo: {JsonConvert.SerializeObject(balanceDTO)}; Error: {ex.Message}");
                return balanceDTO;
            }
        }

        public async Task<AccountBalanceByCurrencyDTO> BalanceAsync(AccountBalanceByCurrencyDTO balanceDTO)
        {
            try
            {
                var in1 = new SqlParameter
                {
                    ParameterName = "@_CurrencyType",
                    DbType = System.Data.DbType.Int32,
                    Direction = System.Data.ParameterDirection.Input,
                    SqlValue = CurrencyType
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
                _Log.LogInformation($"EXEC SP_Account_GetBalance_ByCurrencyType {CurrencyType}, {balanceDTO.AccountId}, @_Balance OUTPUT");
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

        private async Task<IEnumerable<TransactionDTO>> GetTransactions(long AccountId)
        {
            ////List<ObjFilter> _Query = new List<ObjFilter>();
            ////_Query.Add(new ObjFilter()
            ////{
            ////    _FieldName = "AccountID",
            ////    _Value = AccountId.ToString(),
            ////    _SearchType = SearchType.Equal,
            ////    _SearchOperator = SearchOperator.And,
            ////    _Type = Payment.Helpers.FieldType.Numeric
            ////});

            ////_Query.Add(new ObjFilter()
            ////{
            ////    _FieldName = "CurrencyType",
            ////    _Value = CurrencyType.ToString(),
            ////    _SearchType = SearchType.Equal,
            ////    _SearchOperator = SearchOperator.And,
            ////    _Type = Payment.Helpers.FieldType.Numeric
            ////});

            ////_Query.Add(new ObjFilter()
            ////{
            ////    _FieldName = "CreatedTime",
            ////    _Value = DateTime.UtcNow.AddDays(-90).ToString("MM/dd/yyyy"),
            ////    _SearchType = SearchType.GreaterThanOrEqual,
            ////    _SearchOperator = SearchOperator.And,
            ////    _Type = Payment.Helpers.FieldType.Datetime
            ////});


            ////return await _elasticClient.SeachTransactionAsync(_Query);
            //////var searchRequest = new SearchRequest<TransactionDTO>();
            //////var products = (await _elasticClient.SearchAsync<TransactionDTO>(searchRequest))
            ////var products = (await _elasticClient.SearchAsync<TransactionDTO>(a => a
            ////            .Index("defaultindex")
            ////           .Size(500)
            ////           //.Query(q => q
            ////           //     .MultiMatch(m => m
            ////           //         .Fields(f => f
            ////           //             .Field("AccountID")
            ////           //             .Field("CurrencyType")
            ////           //         )
            ////           //         .Query($"{AccountId} {CurrencyType}")
            ////           //     )
            ////           // )
            ////           .Query(q => q
            ////                .Match(p => p
            ////                    .Field(f => f.AccountID)
            ////                        .Query(AccountId.ToString()))
            ////               )
            ////           //.Query(q => q
            ////           //     .Match(p => p
            ////           //         .Field(f => f.CurrencyType)
            ////           //             .Query(CurrencyType.ToString()))
            ////           //    )
            ////           ))
            ////    .Documents
            ////    .Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90) && p.CurrencyType == CurrencyType);
            ////return products;

            var products = (await _elasticClient.SearchAsync<TransactionDTO>(a => a
                       .Index("transaction")
                       .Query(q => q
                            .Bool(b => b
                                .Must(
                                    bs => bs.Term(p => p.AccountID, AccountId),
                                    bs => bs.Term(p => p.CurrencyType, CurrencyType),
                                    bs => bs.DateRange(p => p.Field("createdTime").GreaterThanOrEquals(DateTime.UtcNow.AddDays(-90)))
                                )
                            )
                           )
                       ))
                .Documents
                //.Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
                ;
            _Log.LogInformation($"GetTransactions {CurrencyType}, {AccountId}, {products.Count()} OUTPUT");//{JsonConvert.SerializeObject(products)}
            return products;
        }

        public async Task<IEnumerable<TransactionDTO>> TransactionAsync(long AccountId, int count, int skip = 0)
        {
            var products = (await GetTransactions(AccountId))
                .OrderByDescending(p => p.CreatedTime)
                .Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
                .Skip(skip)
                .Take(count)
                ;
            _Log.LogInformation($"List transaction {CurrencyType}, {AccountId}, {count}, {skip}, {products.Count()} OUTPUT");//{JsonConvert.SerializeObject(products)}
            return products;
        }

        public async Task<TransactionDTO> TransactionByIdAsync(long AccountId, string Id)
        {
            var product = (await GetTransactions(AccountId))
              .Where(p => p.CreatedTime >= DateTime.UtcNow.AddDays(-90))
              .FirstOrDefault(p => p.Id == Id);
            _Log.LogInformation($"TransactionByIdAsync {CurrencyType}, {AccountId}, {Id}, {JsonConvert.SerializeObject(product)} OUTPUT");
            return product;
        }

        private async Task<bool> TransactionAddAsync(long AccountId, TransactionDTO product)
        {
            var a = Newtonsoft.Json.Linq.JObject.Parse(JsonConvert.SerializeObject(product));
            if ((await GetTransactions(AccountId)).Any(p => p.Id == product.Id))
            {
                var index = await _elasticClient.UpdateAsync<TransactionDTO>(product, u => u.Doc(product));
                _Log.LogInformation($"Update transaction {CurrencyType}, {AccountId}, {JsonConvert.SerializeObject(product)}, {index.IsValid} OUTPUT");
                if (index.IsValid)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var index = await _elasticClient.IndexDocumentAsync<TransactionDTO>(product);
                _Log.LogInformation($"Insert transaction {CurrencyType}, {AccountId}, {JsonConvert.SerializeObject(product)}, {index.IsValid} OUTPUT");
                if (index.IsValid)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public async Task<TransactionDTO> AddAsync(InputTransactionDTO input)
        {
            #region Exec store
            // Output param
            #region Output Param
            var ResponseStatus = new SqlParameter
            {
                ParameterName = "@_ResponseStatus",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Output
            };
            var Balance = new SqlParameter
            {
                ParameterName = "@_Balance",
                DbType = System.Data.DbType.Int64,
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
                SqlValue = input.ServiceID
            };
            var _CurrencyType = new SqlParameter
            {
                ParameterName = "@_CurrencyType",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = CurrencyType
            };
            var AccountID = new SqlParameter
            {
                ParameterName = "@_AccountID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.AccountID
            };
            var Username = new SqlParameter
            {
                ParameterName = "@_Username",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.Username
            };
            var RelatedAccountID = new SqlParameter
            {
                ParameterName = "@_RelatedAccountID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.RelatedAccountID
            };
            var RelatedUsername = new SqlParameter
            {
                ParameterName = "@_RelatedUsername",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.RelatedUsername
            };
            var CardType = new SqlParameter
            {
                ParameterName = "@_CardType",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.CardType
            };
            var CardID = new SqlParameter
            {
                ParameterName = "@_CardID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.CardID
            };
            var PartnerAmount = new SqlParameter
            {
                ParameterName = "@_PartnerAmount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.PartnerAmount
            };
            var Amount = new SqlParameter
            {
                ParameterName = "@_Amount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.Amount
            };
            var SubAmount = new SqlParameter
            {
                ParameterName = "@_SubAmount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.SubAmount
            };
            var Gift = new SqlParameter
            {
                ParameterName = "@_Gift",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.Gift
            };
            var Tax = new SqlParameter
            {
                ParameterName = "@_Tax",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.Tax
            };
            var Description = new SqlParameter
            {
                ParameterName = "@_Description",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.Description
            };
            var ReferenceID = new SqlParameter
            {
                ParameterName = "@_ReferenceID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.ReferenceID
            };
            var RelatedTranID = new SqlParameter
            {
                ParameterName = "@_RelatedTranID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.RelatedTranID
            };
            var MerchantID = new SqlParameter
            {
                ParameterName = "@_MerchantID",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.MerchantID
            };
            var SourceID = new SqlParameter
            {
                ParameterName = "@_SourceID",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.SourceID
            };
            var ClientIP = new SqlParameter
            {
                ParameterName = "@_ClientIP",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = input.ClientIP
            };
            #endregion
            // Exec store
            await _db.Database.ExecuteSqlRawAsync("EXEC SP_InputTransactions_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19} OUTPUT, {20} OUTPUT",
                    ServiceID, _CurrencyType, AccountID, Username, RelatedAccountID, RelatedUsername, CardType, CardID,
                    PartnerAmount, Amount, SubAmount, Gift, Tax,
                    Description, ReferenceID, RelatedTranID, MerchantID, SourceID, ClientIP, Balance, ResponseStatus);
            #endregion
            #region ElasticSearch
            long TransactionID = long.Parse(ResponseStatus?.Value.ToString() ?? "0");
            _Log.LogInformation(String.Format("EXEC SP_InputTransactions_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19} OUTPUT, {20} OUTPUT",
                    ServiceID, _CurrencyType, AccountID, Username, RelatedAccountID, RelatedUsername, CardType, CardID,
                    PartnerAmount, Amount, SubAmount, Gift, Tax,
                    Description, ReferenceID, RelatedTranID, MerchantID, SourceID, ClientIP, Balance, TransactionID));
            if (TransactionID > 0)
            {
                var a = new TransactionDTO(1)
                {
                    AccountID = input.AccountID,
                    Username = input.Username,
                    TransactionID = TransactionID,
                    CurrencyType = CurrencyType,
                    ServiceID = input.ServiceID,
                    Amount = input.Amount,
                    SubAmount = input.SubAmount,
                    Discount = 0,
                    Tax = input.Tax,
                    Description = input.Description,
                    ReferenceID = input.ReferenceID
                };
                await TransactionAddAsync(input.AccountID, a);
                return a;
            }
            else
            {
                return default;
            }
            #endregion
        }

        public async Task<TransactionDTO> DeductAsync(OutputTransactionDTO output)
        {
            #region Exec store
            // Output param
            #region Output Param
            var ResponseStatus = new SqlParameter
            {
                ParameterName = "@_ResponseStatus",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Output
            };
            var Balance = new SqlParameter
            {
                ParameterName = "@_Balance",
                DbType = System.Data.DbType.Int64,
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
                SqlValue = output.ServiceID
            };
            var _CurrencyType = new SqlParameter
            {
                ParameterName = "@_CurrencyType",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = CurrencyType
            };
            var AccountID = new SqlParameter
            {
                ParameterName = "@_AccountID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.AccountID
            };
            var Username = new SqlParameter
            {
                ParameterName = "@_Username",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.Username
            };
            var RelatedAccountID = new SqlParameter
            {
                ParameterName = "@_RelatedAccountID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.RelatedAccountID
            };
            var RelatedUsername = new SqlParameter
            {
                ParameterName = "@_RelatedUsername",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.RelatedUsername
            };
            var Amount = new SqlParameter
            {
                ParameterName = "@_Amount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.Amount
            };
            var SubAmount = new SqlParameter
            {
                ParameterName = "@_SubAmount",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.SubAmount
            };
            var Discount = new SqlParameter
            {
                ParameterName = "@_Gift",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.Discount
            };
            var Tax = new SqlParameter
            {
                ParameterName = "@_Tax",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.Tax
            };
            var Description = new SqlParameter
            {
                ParameterName = "@_Description",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.Description
            };
            var ReferenceID = new SqlParameter
            {
                ParameterName = "@_ReferenceID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.ReferenceID
            };
            var RelatedTranID = new SqlParameter
            {
                ParameterName = "@_RelatedTranID",
                DbType = System.Data.DbType.Int64,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.RelatedTranID
            };
            var MerchantID = new SqlParameter
            {
                ParameterName = "@_MerchantID",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.MerchantID
            };
            var SourceID = new SqlParameter
            {
                ParameterName = "@_SourceID",
                DbType = System.Data.DbType.Int32,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.SourceID
            };
            var ClientIP = new SqlParameter
            {
                ParameterName = "@_ClientIP",
                DbType = System.Data.DbType.String,
                Direction = System.Data.ParameterDirection.Input,
                SqlValue = output.ClientIP
            };
            #endregion
            // Exec store
            await _db.Database.ExecuteSqlRawAsync("EXEC SP_OutputTransactions_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16} OUTPUT, {17} OUTPUT",
                    ServiceID, _CurrencyType, AccountID, Username, RelatedAccountID, RelatedUsername, Amount, SubAmount, Discount, Tax,
                    Description, ReferenceID, RelatedTranID, MerchantID, SourceID, ClientIP, Balance, ResponseStatus);
            #endregion

            #region ElasticSearch
            long TransactionID = long.Parse(ResponseStatus?.Value.ToString() ?? "0");
            _Log.LogInformation(String.Format("EXEC SP_OutputTransactions_Create {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16} OUTPUT, {17} OUTPUT",
                    ServiceID, _CurrencyType, AccountID, Username, RelatedAccountID, RelatedUsername, Amount, SubAmount, Discount, Tax,
                    Description, ReferenceID, RelatedTranID, MerchantID, SourceID, ClientIP, Balance, TransactionID));
            if (TransactionID > 0)
            {
                var a = new TransactionDTO(-1)
                {
                    AccountID = output.AccountID,
                    Username = output.Username,
                    TransactionID = TransactionID,
                    CurrencyType = CurrencyType,
                    ServiceID = output.ServiceID,
                    Amount = output.Amount,
                    SubAmount = output.SubAmount,
                    Discount = 0,
                    Tax = output.Tax,
                    Description = output.Description,
                    ReferenceID = output.ReferenceID
                };
                await TransactionAddAsync(output.AccountID, a);
                return a;
            }
            else
            {
                return default;
            }
            #endregion
        }
    }
}
