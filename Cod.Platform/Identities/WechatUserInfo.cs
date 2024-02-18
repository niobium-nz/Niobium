namespace Cod.Platform.Identities
{
    public class WechatUserInfo
    {
        public bool Subscribe { get; set; }

        public string Openid { get; set; }

        public string Nickname { get; set; }

        /// <summary>
        /// 用户的性别，值为1时是男性，值为2时是女性，值为0时是未知
        /// </summary>
        public int Sex { get; set; }

        public string Language { get; set; }

        public string City { get; set; }

        public string Province { get; set; }

        public string Country { get; set; }

        public string Headimgurl { get; set; }

        public long SubscribeTime { get; set; }

        public string Unionid { get; set; }

        public string Remark { get; set; }

        public string Groupid { get; set; }

        public string TagidList { get; set; }

        public string SubscribeScene { get; set; }

        public string QrScene { get; set; }

        public string QrSceneStr { get; set; }
    }
}
