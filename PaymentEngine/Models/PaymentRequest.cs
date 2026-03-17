using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Models
{
    public class PaymentRequest
    {
        public string MerchantId { get; init; } = string.Empty;
        public string OrderId { get; init; } = string.Empty;
        public int Amount { get; init; }
        public string Currency { get; init; } = string.Empty;
        public string CardToken { get; init; } = string.Empty;
    }
}
