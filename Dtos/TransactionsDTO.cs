using System.Collections.Generic;

namespace Payment.Dtos
{
    public class TransactionsDTO : LstPage
    {
        public TransactionsDTO()
        {
            Transactions = new List<TransactionDTO>();
        }

        public List<TransactionDTO> Transactions { get; set; }
    }

    public class LstPage
    {
        public LstPage()
        {
            TotalCount = 0;
            Page = 1;
            PageSize = 50;
        }
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
