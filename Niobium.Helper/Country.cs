namespace Niobium
{
    public struct Country : IEquatable<Country>
    {
        private static readonly Dictionary<string, string> alpha2 = new()
        {
            {"Albania", "AL" },
            {"Algeria", "DZ" },
            {"Andorra", "AD" },
            {"Angola", "AO" },
            {"Antigua and Barbuda", "AG" },
            {"Argentina", "AR" },
            {"Armenia", "AM" },
            {"Australia", "AU" },
            {"Austria", "AT" },
            {"Azerbaijan", "AZ" },
            {"Bahamas", "BS" },
            {"Bahrain", "BH" },
            {"Bangladesh", "BD" },
            {"Barbados", "BB" },
            {"Belarus", "BY" },
            {"Belgium", "BE" },
            {"Belize", "BZ" },
            {"Benin", "BJ" },
            {"Bhutan", "BT" },
            {"Bolivia", "BO" },
            {"Bosnia and Herzegovina", "BA" },
            {"Botswana", "BW" },
            {"Brazil", "BR" },
            {"Brunei Darussalam", "BN" },
            {"Bulgaria", "BG" },
            {"Burkina Faso", "BF" },
            {"Burundi", "BI" },
            {"Cabo Verde", "CV" },
            {"Cambodia", "KH" },
            {"Cameroon", "CM" },
            {"Canada", "CA" },
            {"Central Africa", "CF" },
            {"Chad", "TD" },
            {"Chile", "CL" },
            {"China", "CN" },
            {"Colombia", "CO" },
            {"Comoros", "KM" },
            {"Congo, Republic of", "CG" },
            {"Congo, Democratic Republic of", "CD" },
            {"Costa Rica", "CR" },
            {"Croatia", "HR" },
            {"Cuba", "CU" },
            {"Cyprus", "CY" },
            {"Czechia", "CZ" },
            {"Denmark", "DK" },
            {"Djibouti", "DJ" },
            {"Ecuador", "EC" },
            {"Egypt", "EG" },
            {"El Salvador", "SV" },
            {"Equatorial Guinea", "GQ" },
            {"Eritrea", "ER" },
            {"Estonia", "EE" },
            {"Eswatini", "SZ" },
            {"Ethiopia", "ET" },
            {"Fiji", "FJ" },
            {"Finland", "FI" },
            {"France", "FR" },
            {"Gabon", "GA" },
            {"Gambia", "GM" },
            {"Georgia", "GE" },
            {"Germany", "DE" },
            {"Ghana", "GH" },
            {"Greece", "GR" },
            {"Grenada", "GD" },
            {"Guatemala", "GT" },
            {"Guinea", "GN" },
            {"Guinea-Bissau", "GW" },
            {"Guyana", "GY" },
            {"Haiti", "HT" },
            {"Honduras", "HN" },
            {"Hungary", "HU" },
            {"Iceland", "IS" },
            {"India", "IN" },
            {"Indonesia", "ID" },
            {"Iran", "IR" },
            {"Iraq", "IQ" },
            {"Ireland", "IE" },
            {"Israel", "IL" },
            {"Italy", "IT" },
            {"Jamaica", "JM" },
            {"Japan", "JP" },
            {"Jordan", "JO" },
            {"Kazakhstan", "KZ" },
            {"Kenya", "KE" },
            {"Kiribati", "KI" },
            {"Korea", "KR" },
            {"Kuwait", "KW" },
            {"Kyrgyzstan", "KG" },
            {"Laos", "LA" },
            {"Latvia", "LV" },
            {"Lebanon", "LB" },
            {"Lesotho", "LS" },
            {"Liberia", "LR" },
            {"Libya", "LY" },
            {"Liechtenstein", "LI" },
            {"Lithuania", "LT" },
            {"Luxembourg", "LU" },
            {"North Macedonia", "MK" },
            {"Madagascar", "MG" },
            {"Malawi", "MW" },
            {"Malaysia", "MY" },
            {"Maldives", "MV" },
            {"Mali", "ML" },
            {"Malta", "MT" },
            {"Marshall Islands", "MH" },
            {"Mauritania", "MR" },
            {"Mauritius", "MU" },
            {"Mexico", "MX" },
            {"Micronesia", "FM" },
            {"Moldova", "MD" },
            {"Monaco", "MC" },
            {"Mongolia", "MN" },
            {"Montenegro", "ME" },
            {"Morocco", "MA" },
            {"Mozambique", "MZ" },
            {"Myanmar ", "MM" },
            {"Namibia", "NA" },
            {"Nauru", "NR" },
            {"Nepal", "NP" },
            {"Netherlands", "NL" },
            {"New Zealand", "NZ" },
            {"Nicaragua", "NI" },
            {"Niger", "NE" },
            {"Nigeria", "NG" },
            {"Norway", "NO" },
            {"Oman", "OM" },
            {"Pakistan", "PK" },
            {"Palau", "PW" },
            {"Panama", "PA" },
            {"Papua New Guinea", "PG" },
            {"Paraguay", "PY" },
            {"Peru", "PE" },
            {"Philippines", "PH" },
            {"Poland", "PL" },
            {"Portugal", "PT" },
            {"Qatar", "QA" },
            {"Romania", "RO" },
            {"Russian Federation", "RU" },
            {"Rwanda", "RW" },
            {"Saint Kitts and Nevis", "KN" },
            {"Saint Lucia", "LC" },
            {"Saint Vincent and the Grenadines", "VC" },
            {"Samoa", "WS" },
            {"San Marino", "SM" },
            {"Sao Tome and Principe", "ST" },
            {"Saudi Arabia", "SA" },
            {"Senegal", "SN" },
            {"Serbia", "RS" },
            {"Seychelles", "SC" },
            {"Sierra Leone", "SL" },
            {"Singapore", "SG" },
            {"Slovakia", "SK" },
            {"Slovenia", "SI" },
            {"Solomon Islands", "SB" },
            {"Somalia", "SO" },
            {"South Africa", "ZA" },
            {"South Sudan", "SS" },
            {"Spain", "ES" },
            {"Sri Lanka", "LK" },
            {"Sudan", "SD" },
            {"Suriname", "SR" },
            {"Sweden", "SE" },
            {"Switzerland", "CH" },
            {"Syria", "SY" },
            {"Tajikistan", "TJ" },
            {"Tanzania", "TZ" },
            {"Thailand", "TH" },
            {"Timor-Leste", "TL" },
            {"Togo", "TG" },
            {"Tonga", "TO" },
            {"Trinidad and Tobago", "TT" },
            {"Tunisia", "TN" },
            {"Turkey", "TR" },
            {"Turkmenistan", "TM" },
            {"Tuvalu", "TV" },
            {"Uganda", "UG" },
            {"Ukraine", "UA" },
            {"United Arab Emirates", "AE" },
            {"United Kingdom", "GB" },
            {"United States", "US" },
            {"Uruguay", "UY" },
            {"Uzbekistan", "UZ" },
            {"Vanuatu", "VU" },
            {"Venezuela", "VE" },
            {"Vietnam", "VN" },
            {"Yemen", "YE" },
            {"Zambia", "ZM" },
            {"Zimbabwe", "ZW" }
        };

        private static readonly Dictionary<string, string> alpha3 = new()
        {
            {"Albania", "ALB" },
            {"Algeria", "DZA" },
            {"Andorra", "AND" },
            {"Angola", "AGO" },
            {"Antigua and Barbuda", "ATG" },
            {"Argentina", "ARG" },
            {"Armenia", "ARM" },
            {"Australia", "AUS" },
            {"Austria", "AUT" },
            {"Azerbaijan", "AZE" },
            {"Bahamas", "BHS" },
            {"Bahrain", "BHR" },
            {"Bangladesh", "BGD" },
            {"Barbados", "BRB" },
            {"Belarus", "BLR" },
            {"Belgium", "BEL" },
            {"Belize", "BLZ" },
            {"Benin", "BEN" },
            {"Bhutan", "BTN" },
            {"Bolivia", "BOL" },
            {"Bosnia and Herzegovina", "BIH" },
            {"Botswana", "BWA" },
            {"Brazil", "BRA" },
            {"Brunei Darussalam", "BRN" },
            {"Bulgaria", "BGR" },
            {"Burkina Faso", "BFA" },
            {"Burundi", "BDI" },
            {"Cabo Verde", "CPV" },
            {"Cambodia", "KHM" },
            {"Cameroon", "CMR" },
            {"Canada", "CAN" },
            {"Central Africa", "CAF" },
            {"Chad", "TCD" },
            {"Chile", "CHL" },
            {"China", "CHN" },
            {"Colombia", "COL" },
            {"Comoros", "COM" },
            {"Congo, Republic of", "COG" },
            {"Congo, Democratic Republic of", "COD" },
            {"Costa Rica", "CRI" },
            {"Croatia", "HRV" },
            {"Cuba", "CUB" },
            {"Cyprus", "CYP" },
            {"Czechia", "CZE" },
            {"Denmark", "DNK" },
            {"Djibouti", "DJI" },
            {"Ecuador", "ECU" },
            {"Egypt", "EGY" },
            {"El Salvador", "SLV" },
            {"Equatorial Guinea", "GNQ" },
            {"Eritrea", "ERI" },
            {"Estonia", "EST" },
            {"Eswatini", "SWZ" },
            {"Ethiopia", "ETH" },
            {"Fiji", "FJI" },
            {"Finland", "FIN" },
            {"France", "FRA" },
            {"Gabon", "GAB" },
            {"Gambia", "GMB" },
            {"Georgia", "GEO" },
            {"Germany", "DEU" },
            {"Ghana", "GHA" },
            {"Greece", "GRC" },
            {"Grenada", "GRD" },
            {"Guatemala", "GTM" },
            {"Guinea", "GIN" },
            {"Guinea-Bissau", "GNB" },
            {"Guyana", "GUY" },
            {"Haiti", "HTI" },
            {"Honduras", "HND" },
            {"Hungary", "HUN" },
            {"Iceland", "ISL" },
            {"India", "IND" },
            {"Indonesia", "IDN" },
            {"Iran", "IRN" },
            {"Iraq", "IRQ" },
            {"Ireland", "IRL" },
            {"Israel", "ISR" },
            {"Italy", "ITA" },
            {"Jamaica", "JAM" },
            {"Japan", "JPN" },
            {"Jordan", "JOR" },
            {"Kazakhstan", "KAZ" },
            {"Kenya", "KEN" },
            {"Kiribati", "KIR" },
            {"Korea", "KOR" },
            {"Kuwait", "KWT" },
            {"Kyrgyzstan", "KGZ" },
            {"Laos", "LAO" },
            {"Latvia", "LVA" },
            {"Lebanon", "LBN" },
            {"Lesotho", "LSO" },
            {"Liberia", "LBR" },
            {"Libya", "LBY" },
            {"Liechtenstein", "LIE" },
            {"Lithuania", "LTU" },
            {"Luxembourg", "LUX" },
            {"North Macedonia", "MKD" },
            {"Madagascar", "MDG" },
            {"Malawi", "MWI" },
            {"Malaysia", "MYS" },
            {"Maldives", "MDV" },
            {"Mali", "MLI" },
            {"Malta", "MLT" },
            {"Marshall Islands", "MHL" },
            {"Mauritania", "MRT" },
            {"Mauritius", "MUS" },
            {"Mexico", "MEX" },
            {"Micronesia", "FSM" },
            {"Moldova", "MDA" },
            {"Monaco", "MCO" },
            {"Mongolia", "MNG" },
            {"Montenegro", "MNE" },
            {"Morocco", "MAR" },
            {"Mozambique", "MOZ" },
            {"Myanmar ", "MMR" },
            {"Namibia", "NAM" },
            {"Nauru", "NRU" },
            {"Nepal", "NPL" },
            {"Netherlands", "NLD" },
            {"New Zealand", "NZL" },
            {"Nicaragua", "NIC" },
            {"Niger", "NER" },
            {"Nigeria", "NGA" },
            {"Norway", "NOR" },
            {"Oman", "OMN" },
            {"Pakistan", "PAK" },
            {"Palau", "PLW" },
            {"Panama", "PAN" },
            {"Papua New Guinea", "PNG" },
            {"Paraguay", "PRY" },
            {"Peru", "PER" },
            {"Philippines", "PHL" },
            {"Poland", "POL" },
            {"Portugal", "PRT" },
            {"Qatar", "QAT" },
            {"Romania", "ROU" },
            {"Russian Federation", "RUS" },
            {"Rwanda", "RWA" },
            {"Saint Kitts and Nevis", "KNA" },
            {"Saint Lucia", "LCA" },
            {"Saint Vincent and the Grenadines", "VCT" },
            {"Samoa", "WSM" },
            {"San Marino", "SMR" },
            {"Sao Tome and Principe", "STP" },
            {"Saudi Arabia", "SAU" },
            {"Senegal", "SEN" },
            {"Serbia", "SRB" },
            {"Seychelles", "SYC" },
            {"Sierra Leone", "SLE" },
            {"Singapore", "SGP" },
            {"Slovakia", "SVK" },
            {"Slovenia", "SVN" },
            {"Solomon Islands", "SLB" },
            {"Somalia", "SOM" },
            {"South Africa", "ZAF" },
            {"South Sudan", "SSD" },
            {"Spain", "ESP" },
            {"Sri Lanka", "LKA" },
            {"Sudan", "SDN" },
            {"Suriname", "SUR" },
            {"Sweden", "SWE" },
            {"Switzerland", "CHE" },
            {"Syria", "SYR" },
            {"Tajikistan", "TJK" },
            {"Tanzania", "TZA" },
            {"Thailand", "THA" },
            {"Timor-Leste", "TLS" },
            {"Togo", "TGO" },
            {"Tonga", "TON" },
            {"Trinidad and Tobago", "TTO" },
            {"Tunisia", "TUN" },
            {"Turkey", "TUR" },
            {"Turkmenistan", "TKM" },
            {"Tuvalu", "TUV" },
            {"Uganda", "UGA" },
            {"Ukraine", "UKR" },
            {"United Arab Emirates", "ARE" },
            {"United Kingdom", "GBR" },
            {"United States", "USA" },
            {"Uruguay", "URY" },
            {"Uzbekistan", "UZB" },
            {"Vanuatu", "VUT" },
            {"Venezuela", "VEN" },
            {"Vietnam", "VNM" },
            {"Yemen", "YEM" },
            {"Zambia", "ZMB" },
            {"Zimbabwe", "ZWE" }
        };

        public static readonly Country China = Parse("CN");
        public static readonly Country UnitedStates = Parse("US");
        public static readonly Country Australia = Parse("AU");
        public static readonly Country NewZealand = Parse("NZ");

        public string Fullname { get; private set; }

        public string Alpha2 => alpha2[Fullname];

        public string Alpha3 => alpha3[Fullname];

        public static bool TryParse(string nameOrCode, out Country result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(nameOrCode))
            {
                return false;
            }
            nameOrCode = nameOrCode.Trim();

            if (alpha2.ContainsKey(nameOrCode))
            {
                result = new Country { Fullname = nameOrCode };
            }

            if (result == default)
            {
                foreach (string key in alpha2.Keys)
                {
                    if (alpha2[key] == nameOrCode)
                    {
                        result = new Country { Fullname = key };
                    }
                }
            }
            if (result == default)
            {
                foreach (string key in alpha3.Keys)
                {
                    if (alpha3[key] == nameOrCode)
                    {
                        result = new Country { Fullname = key };
                    }
                }
            }
            return result != default;
        }

        public static Country Parse(string nameOrCode)
        {
            return !TryParse(nameOrCode, out Country result) ? throw new ArgumentOutOfRangeException(nameof(nameOrCode)) : result;
        }

        public override bool Equals(object? obj)
        {
            return obj is Country country && Equals(country);
        }

        public bool Equals(Country other)
        {
            return Fullname == other.Fullname;
        }

        public override int GetHashCode()
        {
            return 558414575 + EqualityComparer<string>.Default.GetHashCode(Fullname);
        }

        public override string ToString()
        {
            return Fullname;
        }

        public static bool operator ==(Country left, Country right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Country left, Country right)
        {
            return !(left == right);
        }
    }
}
