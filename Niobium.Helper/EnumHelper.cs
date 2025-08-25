namespace Niobium
{
    public static class EnumHelper
    {
        public static IEnumerable<T> ForEach<T>()
        {
            return Enum.GetValues(typeof(T)).Cast<T>();
        }
    }
}
