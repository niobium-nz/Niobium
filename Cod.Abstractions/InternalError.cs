using System;
using System.Collections.Generic;

namespace Cod
{
    public abstract class InternalError
    {
        private const string KeyPrefix = "ERROR_";
        private static readonly List<IErrorRetriever> errorRetrievers;

        public const int Success = 0;

        public const int BadRequest = 400;
        public const int AuthenticationRequired = 401;
        public const int PaymentRequired = 402;
        public const int Forbidden = 403;
        public const int NotFound = 404;
        public const int NotAllowed = 405;
        public const int NotAcceptable = 406;
        public const int RequestTimeout = 408;
        public const int Conflict = 409;
        public const int PreconditionFailed = 412;
        public const int Locked = 423;
        public const int UpgradeRequired = 426;
        public const int TooManyRequests = 429;

        public const int InternalServerError = 500;
        public const int BadGateway = 502;
        public const int ServiceUnavailable = 503;
        public const int GatewayTimeout = 504;

        public const int NetworkFailure = 900;

        public const int Unknown = 999;

        static InternalError() => errorRetrievers = new List<IErrorRetriever>
        {
            new InternalErrorRetriever(),
        };

        public static string UnknownErrorMessage
        {
            get
            {
                if (TryGet(Unknown, out var val))
                {
                    return val;
                }

                throw new NotImplementedException();
            }
        }

        public static void Register(IErrorRetriever retriever) => errorRetrievers.Add(retriever);

        public static bool TryGet(int code, out string value)
        {
            foreach (var errorRetriever in errorRetrievers)
            {
                if (errorRetriever.TryGet($"{KeyPrefix}{code}", out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static string Get(int code)
        {
            if (TryGet(code, out var val))
            {
                return val;
            }

            throw new KeyNotFoundException();
        }
    }
}
