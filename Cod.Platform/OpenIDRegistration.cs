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

        public string Credentials { get; set; }

        public bool OverrideIfExists { get; set; }

        public string OffsetPrefix { get; set; }

        public static IEnumerable<OpenIDRegistration> Build(string mobile)
        {
            return new[]
            {
                new OpenIDRegistration
                {
                    Identity = mobile,
                    Kind = (int)OpenIDKind.PhoneCall,
                },
                new OpenIDRegistration
                {
                    Identity = mobile,
                    Kind = (int)OpenIDKind.SMS,
                },
            };
        }

        public static IEnumerable<OpenIDRegistration> Build(OpenIDKind kind, string app, string openID, string credentials)
        {
            return new[]
            {
                new OpenIDRegistration
                {
                    Identity = openID,
                    Kind = (int)kind,
                    App = app,
                    Credentials = credentials,
                },
            };
        }

        public static IEnumerable<OpenIDRegistration> Build(string mobile, OpenIDKind kind, string app, string openID)
        {
            return new[]
            {
                new OpenIDRegistration
                {
                    Identity = mobile,
                    Kind = (int)OpenIDKind.PhoneCall,
                },
                new OpenIDRegistration
                {
                    Identity = mobile,
                    Kind = (int)OpenIDKind.SMS,
                },
                new OpenIDRegistration
                {
                    Identity = openID,
                    Kind = (int)kind,
                    App = app,
                },
            };
        }
    }
}
