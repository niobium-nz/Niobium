using System;
using System.Collections.Generic;
using System.Linq;

namespace Cod.Platform
{
    internal class CreateWindcaveSessionResponse
    {
        public string ID { get; set; }

        public string State { get; set; }

        public string Type { get; set; }

        public PaymentKind GetKind() => WindcaveHelper.FromWindcaveType(this.Type);

        public string Amount { get; set; }

        public int GetAmount() => (int)(Double.Parse(this.Amount) * 100);

        public string Currency { get; set; }

        public Currency GetCurrency() => Cod.Currency.Parse(this.Currency);

        public string MerchantReference { get; set; }

        public string Expires { get; set; }

        public DateTimeOffset GetExpires() => DateTimeOffset.Parse(this.Expires);

        public bool StoreCard { get; set; }

        public string ClientType { get; set; }

        public string[] Methods { get; set; }

        public List<WindcaveTransaction> Transactions { get; set; }

        public List<WindcaveLink> Links { get; set; }

        public string GetSubmitCardLink() => this.Links != null && this.Links.Any(l => l.Rel == "submitCard") ? this.Links.Single(l => l.Rel == "submitCard").HREF : null;
    }
}
