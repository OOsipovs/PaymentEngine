using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PaymentEngine.Provider
{
    /// <summary>
    /// The JSON body sent to POST /charges on the provider API.
    /// </summary>
    public class ProviderChargeRequest
    {
        [JsonPropertyName("amount")]
        public int Amount { get; init; }

        [JsonPropertyName("currency")]
        public string Currency { get; init; } = string.Empty;

        [JsonPropertyName("card_token")]
        public string CardToken { get; init; } = string.Empty;
    }
}
