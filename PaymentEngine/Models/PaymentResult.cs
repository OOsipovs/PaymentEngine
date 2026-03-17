using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaymentEngine.Models
{
    public enum PaymentStatus
    {
        Approved,
        ThreeDsRequired,
        Declined,
        ValidationError,
        ProviderError,
        UnexpectedResponse
    }

    public class PaymentResult
    {
        public PaymentStatus Status { get; init; }
        public string? SystemOrderRef { get; init; }
        public string? ThreeDsUrl { get; init; }
        public string? DeclineReason { get; init; }
        public string? ErrorMessage { get; init; }

    }
}
