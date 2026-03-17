using PaymentEngine.Provider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Tests
{

    public class PaymentProviderStub : IPaymentProvider
    {
        public ProviderResult Result { get; set; } = ProviderResult.NetworkFailure("not configured");

        public Task<ProviderResult> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Result);
        }
    }
}
