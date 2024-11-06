namespace Cod
{
    public class ValidationState
    {
        private readonly Dictionary<string, List<string>> errors = new();

        public IReadOnlyCollection<string> this[string key] => errors[key];

        public bool IsValid => Count == 0;

        public IEnumerable<string> Keys => errors.Keys;

        public IEnumerable<IReadOnlyCollection<string>> Values => errors.Values;

        public int Count => errors.Count;

        public void AddError(string propertyName, string errorMessage)
        {
            if (!errors.ContainsKey(propertyName))
            {
                errors.Add(propertyName, new List<string> { errorMessage });
            }
            else
            {
                errors[propertyName].Add(errorMessage);
            }
        }

        public bool ContainsKey(string key)
        {
            return errors.ContainsKey(key);
        }

        public bool TryGetValue(string key, out IReadOnlyCollection<string> value)
        {
            bool b = errors.TryGetValue(key, out List<string> result);
            value = result;
            return b;
        }

        public IReadOnlyDictionary<string, IEnumerable<string>> ToDictionary()
        {
            return errors.ToDictionary(kv => kv.Key, kv => kv.Value.AsEnumerable());
        }

        public void Clear() => errors.Clear();
    }
}
