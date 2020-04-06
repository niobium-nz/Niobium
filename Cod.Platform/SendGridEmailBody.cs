using System.Collections.Generic;

namespace Cod.Platform
{
    public class SendGridEmailBody
    {
        public IEnumerable<SendGridEmailAddress> To { get; set; }

        public IDictionary<string, string> DynamicTemplateData { get; set; }

        public string Subject { get; set; }
    }
}
