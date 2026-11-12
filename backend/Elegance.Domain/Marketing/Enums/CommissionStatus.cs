namespace Elegance.Domain.Marketing.Enums;

public enum CommissionStatus
{
    // Order placed, not yet past the return window (or not yet Delivered at all).
    Pending,

    // An admin has manually reviewed/vetted this commission - optional, does not by itself make
    // it payable. The background worker promotes straight from Pending to Payable once the order
    // clears its return window, whether or not this was ever set.
    Approved,

    // Can be allocated against a PromoterPayment now - reached automatically (worker, once past
    // the return window) or manually (an admin's "Mark payable" override, e.g. a trusted promoter).
    Payable,

    PartiallyPaid,
    Paid,

    // The underlying order was returned/refunded - set automatically by AdminOrderService when
    // that happens to any commission not already Paid.
    Reversed,

    // An admin voided this commission for a reason unrelated to a refund (mistake, fraud, etc).
    Cancelled,
}
