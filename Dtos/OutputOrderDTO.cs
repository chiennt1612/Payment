using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Payment.Dtos
{
    public class OrderOutput
    {
        public int ServiceID { get; set; }
        public long MerchantID { get; set; }
        public long Amount { get; set; }
        public long Discount { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
        public long ProductID { get; set; }
        public int Status { get; set; }
    }
    public class OutputOrderDTO
    {
        public int ServiceID;//{ get; set; }
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
        public long ProductID;// { get; set; }
        public int MerchantID;// { get; set; }
        public int SourceID;// { get; set; }
        public int Status { get; set; }
        public string ClientIP;// { get; set; }
        public OutputOrderDTO(long AccountID, string Username)
        {
            this.AccountID = AccountID;
            this.Username = Username;

            this.ServiceID = 1;
            this.RelatedAccountID = 0;
            this.RelatedUsername = "";
            this.SubAmount = 0;
            this.Tax = 0;
            this.ProductID = 0;
            this.MerchantID = 0;
            this.SourceID = 0;
            this.ClientIP = "";
        }
    }
}
