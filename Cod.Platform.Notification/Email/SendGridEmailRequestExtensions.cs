namespace Cod.Platform.Notification.Email
{
    public static class SendGridEmailRequestExtensions
    {
        public static async Task AddAttachmentAsync(
            this SendGridEmailRequest request,
            Stream input,
            string filename,
            string? contentType = null,
            SendGridEmailAttachmentDisposition disposition = SendGridEmailAttachmentDisposition.Attachment,
            string? contentID = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(input);

            if (input.CanSeek)
            {
                input.Seek(0, SeekOrigin.Begin);
            }

            byte[] buff;
            using (MemoryStream memStream = new())
            {
                await input.CopyToAsync(memStream);
                buff = memStream.ToArray();
            }

            string base64 = Convert.ToBase64String(buff);
            request.Attachments ??= [];
            string dis = disposition switch
            {
                SendGridEmailAttachmentDisposition.Attachment => "attachment",
                SendGridEmailAttachmentDisposition.Inline => "inline",
                _ => throw new NotImplementedException(),
            };
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
