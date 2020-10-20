using System;

namespace Cod.Platform
{
    internal static class WindcaveHelper
    {
        public static string ToWindcaveType(this PaymentKind kind)
            => kind switch
            {
                PaymentKind.Authorize => "auth",
                PaymentKind.Complete => "complete",
                PaymentKind.Charge => "purchase",
                PaymentKind.Void => "void",
                PaymentKind.Refund => "refund",
                PaymentKind.Validate => "validate",
                _ => throw new NotSupportedException(),
            };

        public static PaymentKind FromWindcaveType(this string type)
            => type switch
            {
                "auth" => PaymentKind.Authorize,
                "purchase" => PaymentKind.Charge,
                "complete" => PaymentKind.Complete,
                "void" => PaymentKind.Void,
                "refund" => PaymentKind.Refund,
                "validate" => PaymentKind.Validate,
                _ => throw new NotImplementedException(),
            };
    }
}
