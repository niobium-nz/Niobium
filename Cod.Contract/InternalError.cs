using System;
using System.Collections.Generic;

namespace Cod
{
    public class InternalError
    {
        public const int Success = 0;

        public const int BadRequest = 400;
        public const int PaymentRequired = 402;
        public const int AuthenticationRequired = 401;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int NotAcceptable = 406;
        public const int RequestTimeout = 408;
        public const int Conflict = 409;
        public const int PreconditionFailed = 412;
        public const int Locked = 423;
        public const int UpgradeRequired = 426;
        public const int TooManyRequests = 429;

        public const int InternalServerError = 500;
        public const int ServiceUnavailable = 503;
        public const int GatewayTimeout = 504;

        public const int NetworkFailure = 900;
        
        public const int Unknown = 999;

        public static readonly IDictionary<int, string> Messages;

        static InternalError() => Messages = new Dictionary<int, string>
        {
            { Success, String.Empty },

            { BadRequest, "请求无效" },
            { PaymentRequired, "需要付费" },
            { AuthenticationRequired, "需要登录认证" },
            { Forbidden, "权限不足" },
            { NotFound, "资源不存在" },
            { NotAcceptable, "请求不被接受" },
            { RequestTimeout, "请求超时" },
            { Conflict, "发生冲突" },
            { PreconditionFailed, "未达前置条件" },
            { Locked, "被锁定" },
            { UpgradeRequired, "需要升级" },
            { TooManyRequests, "请求超限" },

            { InternalServerError, "服务器错误" },
            { ServiceUnavailable, "服务暂不可用" },
            { GatewayTimeout, "网关超时" },

            { NetworkFailure, "网络异常" },

            { Unknown, "未知错误" },
        };
    }
}
