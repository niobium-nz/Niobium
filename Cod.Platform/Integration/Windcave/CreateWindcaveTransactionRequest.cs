using System;
using Newtonsoft.Json;

namespace Cod.Platform
{
    internal class CreateWindcaveTransactionRequest
    {
        public CreateWindcaveTransactionRequest(CreditCardTransactionKind kind, Currency currency, int amount, string transactionID = null, string cardID = null)
        {
            this.Type = kind.ToWindcaveType();
            this.Amount = (amount / 100d).ToString();
            this.Currency = currency.ToString();

            if (!String.IsNullOrWhiteSpace(transactionID))
            {
                this.TransactionId = transactionID;
            }
            else
            {
                if (String.IsNullOrWhiteSpace(cardID))
                {
                    throw new ArgumentNullException(nameof(cardID));
                }

                this.CardId = cardID;
            }
        }

        public string Type { get; set; }

        public string Amount { get; set; }

        public string Currency { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string CardId { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string TransactionId { get; set; }

        public string MerchantReference { get; set; }

        public string NotificationUrl { get; set; }
    }
}
