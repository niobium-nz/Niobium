namespace Cod.Platform.Tenant.Wechat
{
    public class JsTicketResult
    {
        public int Errcode { get; set; }

        public string Errmsg { get; set; }

        public string Ticket { get; set; }

        public int ExpiresIn { get; set; }
    }
}
