using System.Collections.Generic;

namespace Cod.Platform
{
    public class SendGridEmailRequest
    {
        public IEnumerable<SendGridEmailBody> Personalizations { get; set; }

        public SendGridEmailAddress From { get; set; }

        public string TemplateId { get; set; }

        public ICollection<SendGridEmailAttachment> Attachments { get; set; }
    }
}
