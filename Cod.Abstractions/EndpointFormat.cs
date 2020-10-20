using System.Collections.Generic;
using System.Linq;

namespace Cod
{
    public class EndpointFormat
    {
        public string Template { get; }

        public EndpointFormat(string template) => this.Template = template;

        public EndpointFormat(string baseUri, string query) => this.Template = $"{baseUri}{query}";

        public string ToString(IReadOnlyDictionary<string, object> parameters)
            => parameters.Aggregate(this.Template, (current, parameter) => current.Replace(parameter.Key, parameter.Value.ToString()));
    }
}
