using PaymentEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine
{
    /// <summary>
    /// The single public entry point of the payment engine.
    /// Callers depend on this interface, never on the concrete PaymentService,
    /// which keeps them decoupled from implementation details.
    /// </summary>
    public interface IPaymentService
    {
        Task<PaymentResult> Call(PaymentRequest request, CancellationToken cancellationToken = default);
    }
}
