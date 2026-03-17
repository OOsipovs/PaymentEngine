using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PaymentEngine.Provider
{
    public class PaymentProvider : IPaymentProvider
    {
        private const string ChargesEndpoint = "/charges";

        private readonly HttpClient _httpClient;

        public PaymentProvider(HttpClient httpClient, string bearerToken)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress ??= new Uri("https://api.paymentprovider.com");
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
        }

        public async Task<ProviderResult> ChargeAsync(
            ProviderChargeRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var httpResponse = await _httpClient.PostAsJsonAsync(
                    ChargesEndpoint, request, cancellationToken);

                var rawBody = await httpResponse.Content.ReadAsStringAsync(cancellationToken);

                var parsed = JsonSerializer.Deserialize<ProviderChargeResponse>(rawBody)
                             ?? new ProviderChargeResponse();

                return httpResponse.IsSuccessStatusCode
                    ? ProviderResult.Success((int)httpResponse.StatusCode, parsed, rawBody)
                    : ProviderResult.Failure((int)httpResponse.StatusCode, parsed, rawBody);
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
            {
                return ProviderResult.NetworkFailure(ex.Message);
            }
        }
    }
}
