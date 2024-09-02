using System.Globalization;
using System.Text;

namespace Cod.Platform.Notification.Wechat
{
    public class WechatNotificationParameter
    {
        public WechatNotificationKeyword First { get; set; }

        public List<WechatNotificationKeyword> Keywords { get; set; }

        public WechatNotificationKeyword Remark { get; set; }

        public string ToJson()
        {
            StringBuilder sb = new();
            int index = 1;
            sb.Append(CultureInfo.InvariantCulture, $"{{\"frist\":{{\"value\":\"{First.Value}\",\"color\":\"{First.Color}\"}},");
            foreach (WechatNotificationKeyword item in Keywords)
            {
                sb.Append(CultureInfo.InvariantCulture, $"\"keyword{index}\":{{\"value\":\"{item.Value}\",\"color\":\"{item.Color}\"}},");
                index++;
            }
            sb.Append(CultureInfo.InvariantCulture, $"\"remark\":{{\"value\":\"{Remark.Value}\",\"color\":\"{Remark.Color}\"}}}}");
            return sb.ToString();
        }
    }

    public class WechatNotificationKeyword
    {
        public WechatNotificationKeyword()
        {
            Color = "#000000";
        }

        public string Value { get; set; }

        public string Color { get; set; }
    }
}
