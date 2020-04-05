using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public abstract class SMSNotificationChannel : INotificationChannel
    {
        public async Task<OperationResult> SendAsync(
            string brand,
            string account,
            NotificationContext context,
            int template,
            IReadOnlyDictionary<string, string> parameters,
            int level = 0)
        {
            if (level != NotificationLevels.SMS)
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            account = account.Replace("-", string.Empty)
                .Replace(" ", string.Empty);
            if (string.IsNullOrWhiteSpace(account))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            if (account[0] == '+' && !account.Substring(1, account.Length - 1).All(Char.IsDigit))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }
            else if (!account.All(Char.IsDigit))
            {
                return OperationResult.Create(InternalError.NotAllowed);
            }

            return await this.SendSMSAsync(brand, account, template, parameters);
        }

        protected abstract Task<OperationResult> SendSMSAsync(
            string brand,
            string mobile,
            int template,
            IReadOnlyDictionary<string, string> parameters);

        protected static bool ValidateChineseMobileNumber(string input)
            => !string.IsNullOrWhiteSpace(input)
            && ((input.Length == 11 && input.All(char.IsDigit)) || input.StartsWith("+86"));
    }
}
