using System;

namespace Cod.Platform
{
    internal static class WindcaveHelper
    {
        public static string ToWindcaveType(this CreditCardTransactionKind kind)
            => kind switch
            {
                CreditCardTransactionKind.Authorize => "auth",
                CreditCardTransactionKind.Complete => "complete",
                CreditCardTransactionKind.Charge => "purchase",
                CreditCardTransactionKind.Void => "void",
                CreditCardTransactionKind.Refund => "refund",
                CreditCardTransactionKind.Validate => "validate",
                _ => throw new NotSupportedException(),
            };

        public static CreditCardTransactionKind FromWindcaveType(this string type)
            => type switch
            {
                "auth" => CreditCardTransactionKind.Authorize,
                "purchase" => CreditCardTransactionKind.Charge,
                "complete" => CreditCardTransactionKind.Complete,
                "void" => CreditCardTransactionKind.Void,
                "refund" => CreditCardTransactionKind.Refund,
                "validate" => CreditCardTransactionKind.Validate,
                _ => throw new NotImplementedException(),
            };
    }
}
