using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Provider
{
    /// <summary>
    /// Internal result of a provider HTTP call.
    /// </summary>
    public class ProviderResult
    {
        public bool IsSuccess { get; init; }
        public int HttpStatusCode { get; init; }
        public ProviderChargeResponse? Response { get; init; }
        public string? RawBody { get; init; }
        public string? TransportError { get; init; }

        public bool IsTransportError => TransportError is not null;

        public static ProviderResult Success(int statusCode, ProviderChargeResponse response, string rawBody)
        {
            return new() { IsSuccess = true, HttpStatusCode = statusCode, Response = response, RawBody = rawBody };
        }
            
        public static ProviderResult Failure(int statusCode, ProviderChargeResponse response, string rawBody)
        {
            return new() { IsSuccess = false, HttpStatusCode = statusCode, Response = response, RawBody = rawBody };
        }

        public static ProviderResult NetworkFailure(string error)
        {
            return new() { IsSuccess = false, HttpStatusCode = 0, TransportError = error };
        }
    }
}
