using Microsoft.Extensions.Logging.Abstractions;
using PaymentEngine.Models;
using PaymentEngine.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Tests
{
    public class PaymentServiceTests
    {
        private static (IPaymentService paymentService, PaymentProviderStub paymentProvider) BuildService()
        {
            var paymentProvider = new PaymentProviderStub();
            var paymentService = new PaymentService(paymentProvider, new NullLogger<PaymentService>());
            return (paymentService, paymentProvider);
        }

        private static PaymentRequest ValidRequest()
        {
            return new PaymentRequest
            {
                MerchantId = "merchant_42",
                OrderId = "ord_abc123",
                Amount = 1500,
                Currency = "EUR",
                CardToken = "tok_visa_4242"
            };
        }

        [Fact]
        public async Task ShouldReturn_Approved_WhenProviderApproves()
        {
            var (service, provider) = BuildService();
            provider.Result = ProviderResult.Success(200,
                new ProviderChargeResponse
                {
                    Status = "approved",
                    SystemOrderRef = "ord_98f7a6b5c4d3",
                    Amount = 1500,
                    Currency = "EUR"
                },
                rawBody: """{"status":"approved","system_order_ref":"ord_98f7a6b5c4d3","amount":1500,"currency":"EUR"}""");

            var result = await service.Call(ValidRequest());

            Assert.Equal(PaymentStatus.Approved, result.Status);
            Assert.Equal("ord_98f7a6b5c4d3", result.SystemOrderRef);
        }

        [Fact]
        public async Task ShouldReturn_ThreeDsRequired_WhenProviderRequiresThreeDs()
        {
            var (service, provider) = BuildService();
            provider.Result = ProviderResult.Success(200,
                new ProviderChargeResponse
                {
                    Status = "threeds_required",
                    SystemOrderRef = "ord_11e2f3a4b5c6",
                    Amount = 1500,
                    Currency = "EUR",
                    ThreeDsUrl = "https://example/3ds/abc123"
                },
                rawBody: """{"status":"threeds_required","system_order_ref":"ord_11e2f3a4b5c6","threeds_url":"https://example/3ds/abc123"}""");

            var result = await service.Call(ValidRequest());

            Assert.Equal(PaymentStatus.ThreeDsRequired, result.Status);
            Assert.Equal("ord_11e2f3a4b5c6", result.SystemOrderRef);
            Assert.Equal("https://example/3ds/abc123", result.ThreeDsUrl);
        }

        [Fact]
        public async Task ShouldReturn_DeclinedStatus_WhenProviderReturns422()
        {
            var (service, provider) = BuildService();
            provider.Result = ProviderResult.Failure(422,
                new ProviderChargeResponse { Status = "error", Reason = "card_declined" },
                rawBody: """{"status":"error","reason":"card_declined"}""");

            var result = await service.Call(ValidRequest());

            Assert.Equal(PaymentStatus.Declined, result.Status);
            Assert.Equal("card_declined", result.DeclineReason);
        }

        [Theory]
        [InlineData("", "ord_abc123", 1500, "EUR", "tok_visa_4242")]   // missing merchant_id
        [InlineData("merchant_42", "", 1500, "EUR", "tok_visa_4242")]   // missing order_id
        [InlineData("merchant_42", "ord_abc123", 0, "EUR", "tok_visa_4242")]   // zero amount
        [InlineData("merchant_42", "ord_abc123", 1500, "XYZ", "tok_visa_4242")] // unsupported currency
        [InlineData("merchant_42", "ord_abc123", 1500, "EUR", "")]     // missing card_token
        public async Task ShouldReturn_ValidationError_WhenRequestIsInvalid(string merchantId, string orderId, int amount, string currency, string cardToken)
        {
            var (service, _) = BuildService();
            var request = new PaymentRequest
            {
                MerchantId = merchantId,
                OrderId = orderId,
                Amount = amount,
                Currency = currency,
                CardToken = cardToken
            };

            var result = await service.Call(request);

            Assert.Equal(PaymentStatus.ValidationError, result.Status);
        }

        [Fact]
        public async Task ShouldReturn_ReturnUnexpected_WhenProviderReturnsUndocumented()
        {
            var (service, provider) = BuildService();
            provider.Result = ProviderResult.Failure(200,
                new ProviderChargeResponse { Status = "pending" },
                rawBody: """{"status":"pending"}""");

            var result = await service.Call(ValidRequest());

            Assert.Equal(PaymentStatus.UnexpectedResponse, result.Status);
        }

        [Fact]
        public async Task ShouldReturn_ProviderError_WhenNetworkFails()
        {
            var (service, provider) = BuildService();
            provider.Result = ProviderResult.NetworkFailure("timeout");

            var result = await service.Call(ValidRequest());

            Assert.Equal(PaymentStatus.ProviderError, result.Status);
        }

        [Fact]
        public async Task ShouldReturn_Unexpected_WhenUndocumentedStatusFromProvider()
        {
            var (service, provider) = BuildService();
            provider.Result = ProviderResult.Failure(503, new ProviderChargeResponse(), rawBody:"Service unavailable");

            var result = await service.Call(ValidRequest());

            Assert.Equal(PaymentStatus.UnexpectedResponse, result.Status);
        }



    }
}
