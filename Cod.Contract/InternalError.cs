using System;
using System.Collections.Generic;

namespace Cod
{
    public class InternalError
    {
        public const int Success = 0;
        public const int Unknown = 5000;
        public const int NetworkFailure = 5003;
        public const int Unauthenticated = 5004;
        public const int Unauthorized = 5005;

        public static readonly IDictionary<int, string> Messages;

        static InternalError() => Messages = new Dictionary<int, string>
        {
            { Success, String.Empty },
            { Unknown, "未知错误" },
            { NetworkFailure, "网络异常" },
            { Unauthenticated, "尚未登录" },
            { Unauthorized, "权限不足" },
        };
    }
}
