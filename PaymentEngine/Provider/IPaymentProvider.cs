using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Provider
{
    /// <summary>
    /// Abstraction over the external payment provider HTTP call.
    /// Having an interface here means tests can swap in a fake without
    /// </summary>
    public interface IPaymentProvider
    {
        Task<ProviderResult> ChargeAsync(ProviderChargeRequest request, CancellationToken cancellationToken = default);
    }
}
