using System;

namespace MVCFlowerShop.Models
{
    public class Payment
    {
        public int ID { get; set; }

        public string CustomerName { get; set; }

        public DateTime PaymentDaten { get; set; }

        public decimal PaymentAmount { get; set; }
    }
}
