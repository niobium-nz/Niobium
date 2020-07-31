using System;
using System.Collections.Generic;

namespace Cod.Platform
{
    public class OpenIDRegistration
    {
        public Guid User { get; set; }

        public int Kind { get; set; }

        public string App { get; set; }

        public string Identity { get; set; }

        public bool OverrideIfExists { get; set; }

        public string OffsetPrefix { get; set; }

        public static IEnumerable<OpenIDRegistration> Build(string mobile) => Build(mobile, Guid.NewGuid());

        public static IEnumerable<OpenIDRegistration> Build(string mobile, Guid userID)
        {
            return new[]
            {
                new OpenIDRegistration
                {
                    User = userID,
                    Identity = mobile,
                    Kind = (int)OpenIDKind.PhoneCall,
                },
                new OpenIDRegistration
                {
                    User = userID,
                    Identity = mobile,
                    Kind = (int)OpenIDKind.SMS,
                },
            };
        }

        public static IEnumerable<OpenIDRegistration> Build(OpenIDKind kind, string app, string openID) => Build(kind, app, openID, Guid.NewGuid());

        public static IEnumerable<OpenIDRegistration> Build(OpenIDKind kind, string app, string openID, Guid userID)
        {
            return new[]
            {
                new OpenIDRegistration
                {
                    User = userID,
                    Identity = openID,
                    Kind = (int)kind,
                    App = app,
                },
            };
        }

        public static IEnumerable<OpenIDRegistration> Build(string mobile, OpenIDKind kind, string app, string openID) => Build(mobile, kind, app, openID, Guid.NewGuid());

        public static IEnumerable<OpenIDRegistration> Build(string mobile, OpenIDKind kind, string app, string openID, Guid userID)
        {
            return new[]
            {
                new OpenIDRegistration
                {
                    User = userID,
                    Identity = mobile,
                    Kind = (int)OpenIDKind.PhoneCall,
                },
                new OpenIDRegistration
                {
                    User = userID,
                    Identity = mobile,
                    Kind = (int)OpenIDKind.SMS,
                },
                new OpenIDRegistration
                {
                    User = userID,
                    Identity = openID,
                    Kind = (int)kind,
                    App = app,
                },
            };
        }
    }
}
