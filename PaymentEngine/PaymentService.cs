using PaymentEngine.Models;
using PaymentEngine.Provider;
using PaymentEngine.Validation;
using Microsoft.Extensions.Logging;

namespace PaymentEngine
{
    /// <summary>
    /// Orchestrates a charge attempt:
    ///   1. Validate the incoming request
    ///   2. Call the provider
    ///   3. PArse and interpret the response)
    ///   4. Log enough context to debug any outcome
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentProvider _provider;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(IPaymentProvider provider, ILogger<PaymentService> logger)
        {
            _provider = provider;
            _logger = logger;
        }

        /// <summary>
        /// Process a charge request and return a structured result.
        ///  all outcomes are capturedin the returned PaymentResult so callers have a stable contract.
        /// </summary>
        public async Task<PaymentResult> Call(PaymentRequest request, CancellationToken cancellationToken = default)
        {
            //Validate request
            var validationError = PaymentRequestValidator.Validate(request);
            if (validationError is not null)
            {
                _logger.LogWarning(
                    "Validation failed for order {OrderId} merchant {MerchantId}: {Reason}",
                    request.OrderId, request.MerchantId, validationError);

                return new PaymentResult { Status = PaymentStatus.ValidationError, ErrorMessage = validationError };

            }

            _logger.LogInformation(
                "Initiating charge: order={OrderId} merchant={MerchantId} amount={Amount} currency={Currency}",
                request.OrderId, request.MerchantId, request.Amount, request.Currency);

            //Call provider
            var chargeRequest = new ProviderChargeRequest
            {
                Amount = request.Amount,
                Currency = request.Currency,
                CardToken = request.CardToken
            };

            ProviderResult providerResult;
            try
            {
                providerResult = await _provider.ChargeAsync(chargeRequest, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected exception calling provider for order {OrderId}", request.OrderId);

                return new PaymentResult { Status = PaymentStatus.UnexpectedResponse, ErrorMessage = "Provider call threw an unexpected exception" };
            }

            //parse response
            return ParseProviderResult(providerResult, request);
        }

        //Helpers
        private PaymentResult ParseProviderResult(ProviderResult result, PaymentRequest request)
        {

            if (result.IsTransportError)
            {
                _logger.LogError(
                    "Transport error for order {OrderId}: {Error}. " +
                    "Charge state is UNKNOWN — manual reconciliation may be required.",
                    request.OrderId, result.TransportError);

                return new PaymentResult { Status = PaymentStatus.ProviderError, ErrorMessage = $"Transport error: {result.TransportError}" };
            }

            var response = result.Response;

            if (result.HttpStatusCode == 200)
            {
                return response?.Status switch
                {
                    "approved" when response.SystemOrderRef is not null =>
                        HandleApproved(response, request),

                    "threeds_required" when response.SystemOrderRef is not null && response.ThreeDsUrl is not null =>
                        HandleThreeDsRequired(response, request),

                    _ => HandleUnexpected(result, request,
                             $"HTTP 200 with unrecognised status='{response?.Status}'")
                };
            }

            if (result.HttpStatusCode == 422)
            {
                var reason = response?.Reason ?? "unknown_reason";

                _logger.LogInformation(
                    "Charge declined for order {OrderId} merchant {MerchantId}: reason={Reason}",
                    request.OrderId, request.MerchantId, reason);

                return new PaymentResult { Status = PaymentStatus.Declined, DeclineReason = reason };
            }

            return HandleUnexpected(result, request,
                $"Undocumented HTTP status {result.HttpStatusCode}");
        }

        private PaymentResult HandleApproved(ProviderChargeResponse response, PaymentRequest request)
        {
            _logger.LogInformation(
                "Charge approved: order={OrderId} merchant={MerchantId} systemRef={SystemRef} amount={Amount} currency={Currency}",
                request.OrderId, request.MerchantId, response.SystemOrderRef,
                response.Amount, response.Currency);

            return new PaymentResult { Status = PaymentStatus.Approved, SystemOrderRef = response.SystemOrderRef! };
        }

        private PaymentResult HandleThreeDsRequired(ProviderChargeResponse response, PaymentRequest request)
        {
            _logger.LogInformation(
                "3DS required for order {OrderId} merchant {MerchantId} systemRef={SystemRef}",
                request.OrderId, request.MerchantId, response.SystemOrderRef);

            return new PaymentResult { Status = PaymentStatus.ThreeDsRequired, SystemOrderRef = response.SystemOrderRef!, ThreeDsUrl = response.ThreeDsUrl! };
        }

        private PaymentResult HandleUnexpected(ProviderResult result, PaymentRequest request, string reason)
        {
            _logger.LogError(
                "Unexpected provider response for order {OrderId} merchant {MerchantId}: {Reason}. " +
                "RawBody={RawBody}",
                request.OrderId, request.MerchantId, reason, result.RawBody);

            return new PaymentResult { Status=PaymentStatus.UnexpectedResponse, ErrorMessage = reason };
        }
    }
}
