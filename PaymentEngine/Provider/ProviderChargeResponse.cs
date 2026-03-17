using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PaymentEngine.Provider
{
    /// <summary>
    /// Represents every field the provider may return.
    /// also all fields are nullable so we can safely deserialise unknown shapes.
    /// </summary>
    public class ProviderChargeResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; init; }

        [JsonPropertyName("system_order_ref")]
        public string? SystemOrderRef { get; init; }

        [JsonPropertyName("amount")]
        public int? Amount { get; init; }

        [JsonPropertyName("currency")]
        public string? Currency { get; init; }

        [JsonPropertyName("threeds_url")]
        public string? ThreeDsUrl { get; init; }

        [JsonPropertyName("reason")]
        public string? Reason { get; init; }
    }
}
