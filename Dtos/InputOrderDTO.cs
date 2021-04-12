using System;
using System.Collections.Generic;

namespace Payment.Dtos
{
    public class OrderPayInput
    {
        public long Amount { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
        public long ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
    }

    public class OrderAddInput
    {
        public long Amount { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
        public long EventID { get; set; }
        public string EventCode { get; set; }
        public string EventName { get; set; }
    }

    public class OrderProcessing
    {
        //public int ServiceID { get; set; }
        public OrderProcessing()
        {
            Order = null;
            Services = null;
        }
        public OrderDTO Order { get; set; }
        public IEnumerable<PayGateServices> Services { get; set; }
    }

    //public class PayStatus
    //{
    //    public string service_name { get; set; }
    //    public string currency_code { get; set; }
    //    public string txn_ref { get; set; }
    //    public string order_info { get; set; }
    //    public int transaction_no { get; set; }
    //    public double amount { get; set; }
    //    public string status { get; set; }
    //    public string pay_date { get; set; }
    //}

    public class ProcessingInput
    {
        public int ServiceID { get; set; }
        public long OrderID { get; set; }
        public DateTime OrderTime { get; set; }
    }

    public class ProcessingOutput
    {
        public OrderDTO Order { get; set; }
        public string Url { get; set; }
    }

    public class InputOrderDTO
    {
        public int ServiceID;
        public string ServiceName;
        public long AccountID { get; set; }
        public string Username { get; set; }
        public long Amount { get; set; }
        public long SubAmount { get; set; }
        public long Discount { get; set; }
        public long Tax { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
        public long ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public InputOrderDTO(long AccountID, string Username, int ServiceID)
        {
            this.AccountID = AccountID;
            this.Username = Username;

            if (!(ServiceID == 1 || ServiceID == 2 || ServiceID == 3)) this.ServiceID = 1; else this.ServiceID = ServiceID;
            this.ServiceName = (this.ServiceID == 1? "Cộng Xu" : (this.ServiceID == 2 ? "Trừ Xu" : "Thanh toán qua cổng Paygate"));
        }
    }
    public class EntityOrder
    {
        public int ServiceID { get; set; }
        public string ServiceName { get; set; }
        public long AccountID { get; set; }
        public string Username { get; set; }
        public long Amount { get; set; }
        public long SubAmount { get; set; }
        public long Discount { get; set; }
        public long Tax { get; set; }
        public string Description { get; set; }
        public long ReferenceID { get; set; }
        public long ProductID { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; }
        public long OrderID { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
