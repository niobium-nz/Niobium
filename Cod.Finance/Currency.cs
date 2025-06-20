using System.Globalization;
using System.Text.Json.Serialization;

namespace Cod.Finance
{
    [JsonConverter(typeof(CurrencyJsonConverter))]
    public struct Currency : IEquatable<Currency>
    {
        private static readonly Dictionary<string, CultureInfo> cultures = new()
        {
            { "NZD", CultureInfo.GetCultureInfo("en-NZ") },
            { "AUD", CultureInfo.GetCultureInfo("en-AU") },
            { "USD", CultureInfo.GetCultureInfo("en-US") },
            { "CNY", CultureInfo.GetCultureInfo("zh-CN") },
        };

        private static readonly string[] codes =
        [
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
        ];

        public static readonly Currency CNY = Currency.Parse("CNY");
        public static readonly Currency USD = Currency.Parse("USD");
        public static readonly Currency AUD = Currency.Parse("AUD");
        public static readonly Currency NZD = Currency.Parse("NZD");

        public static bool TryParse(string code, out Currency result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(code))
            {
                return false;
            }

            code = code.Trim().ToUpperInvariant();
            if (!codes.Contains(code))
            {
                return false;
            }

            result = new Currency { Code = code };
            return true;
        }

        public static Currency Parse(string code)
        {
            _ = code ?? throw new ArgumentNullException(nameof(code));
            if (!TryParse(code, out var result))
            {
                throw new NotSupportedException($"The currency code '{code}' is not supported.");
            }

            return result;
        }

        public static CultureInfo GetCulture(Currency currency)
        {
            return GetCulture(currency.Code);
        }

        public static CultureInfo GetCulture(string code)
        {
            return string.IsNullOrWhiteSpace(code)
                ? throw new ArgumentNullException(nameof(code))
                : !cultures.TryGetValue(code, out CultureInfo? value) ? throw new NotSupportedException() : value;
        }

        public string Code { get; private set; }

        public override bool Equals(object? obj)
        {
            return obj is Currency currency && Equals(currency);
        }

        public bool Equals(Currency other)
        {
            return Code == other.Code;
        }

        public override int GetHashCode()
        {
            return -434485196 + EqualityComparer<string>.Default.GetHashCode(Code);
        }

        public override string ToString()
        {
            return Code;
        }

        public static implicit operator string(Currency currency) => currency.Code;

        public static implicit operator Currency(string code) => Parse(code);

        public static bool operator ==(Currency left, Currency right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Currency left, Currency right)
        {
            return !(left == right);
        }
    }
}
