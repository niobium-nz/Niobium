using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Cod
{
    public struct Currency : IEquatable<Currency>
    {
        private static readonly IReadOnlyDictionary<string, CultureInfo> cultures = new Dictionary<string, CultureInfo>
        {
            { "NZD", CultureInfo.GetCultureInfo("en-NZ") },
            { "AUD", CultureInfo.GetCultureInfo("en-AU") },
            { "USD", CultureInfo.GetCultureInfo("en-US") },
            { "CNY", CultureInfo.GetCultureInfo("zh-CN") },
        };

        private static readonly string[] codes = new string[]
        {
            "AED",
            "AFN",
            "ALL",
            "AMD",
            "ANG",
            "AOA",
            "ARS",
            "AUD",
            "AWG",
            "AZN",
            "BAM",
            "BBD",
            "BDT",
            "BGN",
            "BHD",
            "BIF",
            "BMD",
            "BND",
            "BOB",
            "BOV",
            "BRL",
            "BSD",
            "BTN",
            "BWP",
            "BYN",
            "BZD",
            "CAD",
            "CDF",
            "CHE",
            "CHF",
            "CHW",
            "CLF",
            "CLP",
            "CNY",
            "COP",
            "COU",
            "CRC",
            "CUC",
            "CUP",
            "CVE",
            "CZK",
            "DJF",
            "DKK",
            "DOP",
            "DZD",
            "EGP",
            "ERN",
            "ETB",
            "EUR",
            "FJD",
            "FKP",
            "GBP",
            "GEL",
            "GHS",
            "GIP",
            "GMD",
            "GNF",
            "GTQ",
            "GYD",
            "HKD",
            "HNL",
            "HRK",
            "HTG",
            "HUF",
            "IDR",
            "ILS",
            "INR",
            "IQD",
            "IRR",
            "ISK",
            "JMD",
            "JOD",
            "JPY",
            "KES",
            "KGS",
            "KHR",
            "KMF",
            "KPW",
            "KRW",
            "KWD",
            "KYD",
            "KZT",
            "LAK",
            "LBP",
            "LKR",
            "LRD",
            "LSL",
            "LYD",
            "MAD",
            "MDL",
            "MGA",
            "MKD",
            "MMK",
            "MNT",
            "MOP",
            "MRU",
            "MUR",
            "MVR",
            "MWK",
            "MXN",
            "MXV",
            "MYR",
            "MZN",
            "NAD",
            "NGN",
            "NIO",
            "NOK",
            "NPR",
            "NZD",
            "OMR",
            "PAB",
            "PEN",
            "PGK",
            "PHP",
            "PKR",
            "PLN",
            "PYG",
            "QAR",
            "RON",
            "RSD",
            "RUB",
            "RWF",
            "SAR",
            "SBD",
            "SCR",
            "SDG",
            "SEK",
            "SGD",
            "SHP",
            "SLL",
            "SOS",
            "SRD",
            "SSP",
            "STN",
            "SVC",
            "SYP",
            "SZL",
            "THB",
            "TJS",
            "TMT",
            "TND",
            "TOP",
            "TRY",
            "TTD",
            "TWD",
            "TZS",
            "UAH",
            "UGX",
            "USD",
            "USN",
            "UYI",
            "UYU",
            "UYW",
            "UZS",
            "VES",
            "VND",
            "VUV",
            "WST",
            "XAF",
            "XAG",
            "XAU",
            "XBA",
            "XBB",
            "XBC",
            "XBD",
            "XCD",
            "XDR",
            "XOF",
            "XPD",
            "XPF",
            "XPT",
            "XSU",
            "XTS",
            "XUA",
            "XXX",
            "YER",
            "ZAR",
            "ZMW",
            "ZWL"
        };

        public static Currency Parse(string code)
        {
            _ = code ?? throw new ArgumentNullException(nameof(code));

            code = code.Trim().ToUpper();
            if (!codes.Contains(code))
            {
                throw new NotSupportedException();
            }

            return new Currency { Code = code };
        }

        public static CultureInfo GetCulture(Currency currency)
        {
            return GetCulture(currency.Code);
        }

        public static CultureInfo GetCulture(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException(nameof(code));
            }
            if (!cultures.ContainsKey(code))
            {
                throw new NotSupportedException();
            }
            return cultures[code];
        }

        public string Code { get; private set; }

        public override bool Equals(object obj) => obj is Currency currency && this.Equals(currency);

        public bool Equals(Currency other) => this.Code == other.Code;

        public override int GetHashCode() => -434485196 + EqualityComparer<string>.Default.GetHashCode(this.Code);

        public override string ToString() => this.Code;

        public static bool operator ==(Currency left, Currency right) => left.Equals(right);

        public static bool operator !=(Currency left, Currency right) => !(left == right);
    }
}
