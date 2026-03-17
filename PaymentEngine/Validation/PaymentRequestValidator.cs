using PaymentEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Validation
{
    public static class PaymentRequestValidator
    {
        private static readonly HashSet<string> SupportedCurrencies =
            new(StringComparer.OrdinalIgnoreCase) { "EUR", "USD", "GBP" };

        public static string? Validate(PaymentRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.MerchantId))
                return "merchant_id is required";

            if (string.IsNullOrWhiteSpace(request.OrderId))
                return "order_id is required";

            if (request.Amount <= 0)
                return "amount must be a positive integer";

            if (string.IsNullOrWhiteSpace(request.Currency))
                return "currency is required";

            if (!SupportedCurrencies.Contains(request.Currency))
                return $"currency '{request.Currency}' is not supported";

            if (string.IsNullOrWhiteSpace(request.CardToken))
                return "card_token is required";

            return null;
        }
    }
}
