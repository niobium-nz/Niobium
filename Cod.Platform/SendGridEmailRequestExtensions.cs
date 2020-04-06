using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cod.Platform
{
    public static class SendGridEmailRequestExtensions
    {
        public async static Task AddAttachmentAsync(
            this SendGridEmailRequest request,
            Stream input,
            string filename,
            string contentType = null,
            SendGridEmailAttachmentDisposition disposition = SendGridEmailAttachmentDisposition.Attachment,
            string contentID = null)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.CanSeek)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            byte[] buff;
            using (var memStream = new MemoryStream())
            {
                await input.CopyToAsync(memStream);
                buff = memStream.ToArray();
            }

            var base64 = Convert.ToBase64String(buff);
            if (request.Attachments == null)
            {
                request.Attachments = new List<SendGridEmailAttachment>();
            }

            string dis;
            switch (disposition)
            {
                case SendGridEmailAttachmentDisposition.Attachment:
                    dis = "attachment";
                    break;
                case SendGridEmailAttachmentDisposition.Inline:
                    dis = "inline";
                    break;
                default:
                    throw new NotImplementedException();
            }

            request.Attachments.Add(new SendGridEmailAttachment
            {
                Content = base64,
                ContentId = contentID,
                Disposition = dis,
                Filename = filename,
                Type = contentType,
            });
        }
    }
}
