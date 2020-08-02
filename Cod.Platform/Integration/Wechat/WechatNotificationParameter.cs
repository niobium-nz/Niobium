using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;

namespace Cod.Platform
{
    public class WechatNotificationParameter
    {
        public WechatNotificationKeyword First { get; set; }

        public List<WechatNotificationKeyword> Keywords { get; set; }

        public WechatNotificationKeyword Remark { get; set; }

        public string ToJson()
        {
            var sb = new StringBuilder();
            var index = 1;
            sb.Append($"{{\"frist\":{{\"value\":\"{this.First.Value}\",\"color\":\"{this.First.Color}\"}},");
            foreach (var item in this.Keywords)
            {
                sb.Append($"\"keyword{index}\":{{\"value\":\"{item.Value}\",\"color\":\"{item.Color}\"}},");
                index++;
            }
            sb.Append($"\"remark\":{{\"value\":\"{this.Remark.Value}\",\"color\":\"{this.Remark.Color}\"}}}}");
            return sb.ToString();
        }
    }

    public class WechatNotificationKeyword
    {
        public WechatNotificationKeyword()
        {
            this.Color = "#000000";
        }

        public string Value { get; set; }

        public string Color { get; set; }
    }
}
