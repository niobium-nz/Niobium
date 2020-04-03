using System.Collections.Generic;

namespace Cod
{
    public class ValidationState
    {
        private readonly Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>();

        public IReadOnlyCollection<string> this[string key] => this.errors[key];

        public bool IsValid => this.Count == 0;

        public IEnumerable<string> Keys => this.errors.Keys;

        public IEnumerable<IReadOnlyCollection<string>> Values => this.errors.Values;

        public int Count => this.errors.Count;

        public void AddError(string propertyName, string errorMessage)
        {
            if (!this.errors.ContainsKey(propertyName))
            {
                this.errors.Add(propertyName, new List<string> { errorMessage });
            }
            else
            {
                this.errors[propertyName].Add(errorMessage);
            }
        }

        public bool ContainsKey(string key) => this.errors.ContainsKey(key);

        public bool TryGetValue(string key, out IReadOnlyCollection<string> value)
        {
            var b = this.errors.TryGetValue(key, out var result);
            value = result;
            return b;
        }

        public IDictionary<string, List<string>> ToDictionary() => this.errors;
    }
}
