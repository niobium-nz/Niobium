namespace Cod.Platform
{
    public class WechatTemplateMessageRequest
    {
        public string Touser { get; set; }

        public string TemplateId { get; set; }

        public string Url { get; set; }

        public object Data { get; set; }
    }
}
