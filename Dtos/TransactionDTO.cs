using Nest;
using System;

namespace Payment.Dtos
{
    [ElasticsearchType(RelationName = "transaction")]
    public class TransactionDTO
    {
        public TransactionDTO(int _Dir = 1)
        {
            CreatedTime = DateTime.UtcNow;
            Id = Guid.NewGuid().ToString();
            if(_Dir == 1)
            {
                ServiceID = 1;
                Dir = _Dir;
            }
            else if (_Dir == -1)
            {
                ServiceID = 2;
                Dir = _Dir;
            }
            else
            {
                ServiceID = 3;
                Dir = 1;
            }
        }
        [Number(NumberType.Long)]
        public long TransactionID { get; set; }

        [Number(NumberType.Integer)]
        public int CurrencyType { get; set; }

        [Text]
        public string Id { get; set; }

        [Number(NumberType.Integer)]
        public int ServiceID { get; set; }

        [Number(NumberType.Integer)]
        public int Dir { get; set; }

        [Number(NumberType.Long)]
        public long AccountID { get; set; }

        [Text]
        public string Username { get; set; }

        [Number(NumberType.Long)]
        public long Amount { get; set; }

        [Number(NumberType.Long)]
        public long SubAmount { get; set; }

        [Number(NumberType.Long)]
        public long Discount { get; set; }

        [Number(NumberType.Long)]
        public long Tax { get; set; }

        [Text]
        public string Description { get; set; }

        [Number(NumberType.Long)]
        public long ReferenceID { get; set; }

        [Date]
        public DateTime CreatedTime { get; set; }
    }
}
