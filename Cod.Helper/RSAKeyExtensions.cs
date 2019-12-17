using System;
using System.Security.Cryptography;
using System.Xml;

namespace Cod
{
    public static class RSAKeyExtensions
    {
        #region JSON
        internal static void FromJson(this RSA rsa, RSAParametersJson paramsJson)
        {
            try
            {
                var parameters = new RSAParameters
                {
                    Modulus = paramsJson.Modulus != null ? Convert.FromBase64String(paramsJson.Modulus) : null,
                    Exponent = paramsJson.Exponent != null ? Convert.FromBase64String(paramsJson.Exponent) : null,
                    P = paramsJson.P != null ? Convert.FromBase64String(paramsJson.P) : null,
                    Q = paramsJson.Q != null ? Convert.FromBase64String(paramsJson.Q) : null,
                    DP = paramsJson.DP != null ? Convert.FromBase64String(paramsJson.DP) : null,
                    DQ = paramsJson.DQ != null ? Convert.FromBase64String(paramsJson.DQ) : null,
                    InverseQ = paramsJson.InverseQ != null ? Convert.FromBase64String(paramsJson.InverseQ) : null,
                    D = paramsJson.D != null ? Convert.FromBase64String(paramsJson.D) : null
                };
                rsa.ImportParameters(parameters);
            }
            catch
            {
                throw new Exception("Invalid JSON RSA key.");
            }
        }

        internal static RSAParametersJson ToJson(this RSA rsa, bool includePrivateParameters)
        {
            var parameters = rsa.ExportParameters(includePrivateParameters);

            return new RSAParametersJson()
            {
                Modulus = parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                Exponent = parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                P = parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                Q = parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                DP = parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                DQ = parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                InverseQ = parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                D = parameters.D != null ? Convert.ToBase64String(parameters.D) : null
            };
        }
        #endregion

        #region XML

        public static void FromXmlString(this RSA rsa, string xmlString, string dummy)
        {
            var parameters = new RSAParameters();

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);

            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Exponent": parameters.Exponent = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "P": parameters.P = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "Q": parameters.Q = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DP": parameters.DP = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "DQ": parameters.DQ = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "InverseQ": parameters.InverseQ = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                        case "D": parameters.D = (String.IsNullOrEmpty(node.InnerText) ? null : Convert.FromBase64String(node.InnerText)); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        public static string ToXmlString(this RSA rsa, bool includePrivateParameters, string dummy)
        {
            var parameters = rsa.ExportParameters(includePrivateParameters);

            return String.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                  parameters.Modulus != null ? Convert.ToBase64String(parameters.Modulus) : null,
                  parameters.Exponent != null ? Convert.ToBase64String(parameters.Exponent) : null,
                  parameters.P != null ? Convert.ToBase64String(parameters.P) : null,
                  parameters.Q != null ? Convert.ToBase64String(parameters.Q) : null,
                  parameters.DP != null ? Convert.ToBase64String(parameters.DP) : null,
                  parameters.DQ != null ? Convert.ToBase64String(parameters.DQ) : null,
                  parameters.InverseQ != null ? Convert.ToBase64String(parameters.InverseQ) : null,
                  parameters.D != null ? Convert.ToBase64String(parameters.D) : null);
        }

        #endregion
    }
}
