using Nest;
using System;
using System.ComponentModel.DataAnnotations;

namespace Payment.Dtos
{
    [ElasticsearchType(RelationName = "orders")]
    public class OrderDTO
    {
        public OrderDTO()
        {
            CreatedTime = DateTime.UtcNow;
            Id = Guid.NewGuid().ToString();
            Status = 0;
        }

        [Text]
        [MaxLength(32)]
        public string Id { get; set; }

        [Number(NumberType.Long)]
        public long OrderID { get; set; }

        [Number(NumberType.Integer)]
        public int ServiceID { get; set; }

        [Text]
        [MaxLength(250)]
        public string ServiceName { get; set; }

        [Number(NumberType.Integer)]
        public int ServiceType { get; set; }

        [Number(NumberType.Long)]
        public long AccountID { get; set; }

        [Text]
        [MaxLength(150)]
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
        [MaxLength(250)]
        public string Description { get; set; }

        [Number(NumberType.Long)]
        public long ReferenceID { get; set; }

        [Number(NumberType.Long)]
        public long ProductID { get; set; }

        [Text]
        [MaxLength(30)]
        public string ProductCode { get; set; }

        [Text]
        [MaxLength(200)]
        public string ProductName { get; set; }

        [Date]
        public DateTime CreatedTime { get; set; }

        [Number(NumberType.Integer)]
        public int Status { get; set; }

        [Text]
        [MaxLength(250)]
        public string StatusName { get; set; }

        [Number(NumberType.Integer)]
        public int CurrencyType { get; set; }
    }
}
