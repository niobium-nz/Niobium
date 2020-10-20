using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Platform
{
    internal class WindcaveTransaction
    {
        public string ID { get; set; }

        public string Username { get; set; }

        public bool Authorised { get; set; }

        public bool AllowRetry { get; set; }

        public string ReCo { get; set; }

        public string ResponseText { get; set; }

        public string AuthCode { get; set; }

        public string Type { get; set; }

        public PaymentKind GetKind() => WindcaveHelper.FromWindcaveType(this.Type);

        public string Method { get; set; }

        public string Amount { get; set; }

        public int GetAmount() => (int)(Double.Parse(this.Amount) * 100);

        public string BalanceAmount { get; set; }

        public int GetBalanceAmount() => (int)(Double.Parse(this.BalanceAmount) * 100);

        public string Currency { get; set; }

        public Currency GetCurrency() => Cod.Currency.Parse(this.Currency);

        public string ClientType { get; set; }

        public string MerchantReference { get; set; }

        public string DateTimeUtc { get; set; }

        public DateTimeOffset GetTime() => DateTimeOffset.Parse(this.DateTimeUtc);

        public string SettlementDate { get; set; }

        public List<WindcaveLink> Links { get; set; }

        public string Cvc2ResultCode { get; set; }

        public string StoredCardIndicator { get; set; }

        public string NotificationUrl { get; set; }

        public string SessionId { get; set; }

        public bool IsSurcharge { get; set; }

        public string LiabilityIndicator { get; set; }

        public WindcaveCard Card { get; set; }
    }
}