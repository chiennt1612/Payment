using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Dtos
{
    public class TransactionOutput
    {
        public long Amount { get; set; }
        public long Discount { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
    }
    public class OutputTransactionDTO
    {
        public int ServiceID;//{ get; set; }
        public int CurrencyType;// { get; set; }
        public long AccountID;// { get; set; }
        public string Username;// { get; set; }
        public long RelatedAccountID;// { get; set; }
        public string RelatedUsername;// { get; set; }
        public long Amount { get; set; }
        public long SubAmount;// { get; set; }
        public long Discount { get; set; }
        public long Tax;// { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
        public long RelatedTranID;// { get; set; }
        public int MerchantID;// { get; set; }
        public int SourceID;// { get; set; }
        public string ClientIP;// { get; set; }
        public OutputTransactionDTO(long AccountID, string Username, int CurrencyType)
        {
            this.AccountID = AccountID;
            this.Username = Username;
            this.CurrencyType = CurrencyType;

            this.ServiceID = 1;
            this.RelatedAccountID = 0;
            this.RelatedUsername = "";
            this.SubAmount = 0;
            this.Tax = 0;
            this.RelatedTranID = 0;
            this.MerchantID = 0;
            this.SourceID = 0;
            this.ClientIP = "";
        }
    }
}
