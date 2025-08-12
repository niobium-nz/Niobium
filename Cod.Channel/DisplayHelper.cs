using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Cod.Channel
{
    /// <summary>
    /// Provides helper methods for extracting display properties, actions, and names from view models.
    /// Uses reflection cache for performance.
    /// </summary>
    public static class DisplayHelper
    {
        /// <summary>
        /// Gets a user-friendly display value for an object, using DisplayAttribute if available.
        /// </summary>
        /// <param name="value">The value to display.</param>
        /// <returns>User-friendly display value.</returns>
        public static object? GetDisplayValue(object? value)
        {
            if (value == null)
            {
                return null;
            }

            Type type = value.GetType();
            if (type.IsEnum)
            {
                MemberInfo? memberInfo = type.GetMember(value.ToString() ?? string.Empty).FirstOrDefault();
                if (memberInfo != null)
                {
                    if (memberInfo.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() is DisplayAttribute displayAttribute)
                    {
                        return displayAttribute.GetName();
                    }
                }
            }

            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
                ? value
                : value.ToString();
        }

        /// <summary>
        /// Gets all display properties for a given model, grouped by their display group.
        /// </summary>
        /// <param name="model">The view model instance.</param>
        /// <returns>Grouped display properties.</returns>
        public static IEnumerable<IGrouping<int, DisplayProperty>> GetDisplayProperties(object model)
        {
            Type type = model.GetType();
            PropertyInfo[] properties = ReflectionCacheBootstrapper.PropertyCache.TryGetValue(type, out PropertyInfo[]? cachedProps)
                ? cachedProps
                : type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties
                .Select(p => new
                {
                    Display = p.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute,
                    Control = p.GetCustomAttributes(typeof(DisplayModeAttribute), false).FirstOrDefault() as DisplayModeAttribute,
                    Property = p
                })
                .Where(x => x.Display != null)
                .Select(x => new DisplayProperty(
                    x.Property.Name,
                    x.Display?.GetName(),
                    instance => x.Property.GetValue(instance),
                    order: x.Display?.GetOrder(),
                    group: int.TryParse(x.Display?.GroupName, out int g) ? g : int.MaxValue,
                    description: x.Display?.GetDescription(),
                    isSubject: x.Control?.IsSubject,
                    control: x.Control?.Control))
                .OrderBy(p => p.Order)
                .GroupBy(p => p.Group);
        }

        /// <summary>
        /// Gets all display actions for a given model, ordered by their display order.
        /// </summary>
        /// <param name="model">The view model instance.</param>
        /// <returns>Ordered display actions.</returns>
        public static IEnumerable<DisplayAction> GetDisplayActions(object model)
        {
            Type type = model.GetType();
            MethodInfo[] methods = ReflectionCacheBootstrapper.MethodCache.TryGetValue(type, out MethodInfo[]? cachedMethods)
                ? cachedMethods
                : type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            return methods
                .Select(p => new
                {
                    Action = p.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute,
                    Method = p
                })
                .Where(x => x.Action != null)
                .Select(x => new DisplayAction(
                    x.Action?.GetName(),
                    (i) =>
                    {
                        object? r = x.Method.Invoke(i, null);
                        return r is Task task ? task : Task.CompletedTask;
                    },
                    description: x.Action?.GetDescription(),
                    order: x.Action?.GetOrder()))
                .OrderBy(p => p.Order);
        }

        /// <summary>
        /// Gets the display name for a type, using DisplayAttribute if available.
        /// </summary>
        /// <param name="type">The type to get the display name for.</param>
        /// <returns>Display name or type name.</returns>
        public static string GetDisplayName(Type type)
        {
            DisplayAttribute? attr = ReflectionCacheBootstrapper.TypeDisplayCache.TryGetValue(type, out DisplayAttribute? cachedAttr)
                ? cachedAttr
                : type.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            return attr?.GetName() ?? type.Name;
        }

        /// <summary>
        /// Gets the display name for a property on a model, using DisplayAttribute if available.
        /// </summary>
        /// <typeparam name="TModel">The model type.</typeparam>
        /// <typeparam name="TProperty">The property type.</typeparam>
        /// <param name="model">The model instance.</param>
        /// <param name="expression">Expression selecting the property.</param>
        /// <returns>Display name or property name.</returns>
        public static string GetDisplayName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> expression)
        {
            MemberExpression? member = expression.Body as MemberExpression;
            if (member == null)
            {
                UnaryExpression? unary = expression.Body as UnaryExpression;
                member = unary?.Operand as MemberExpression;
            }
            if (member == null)
            {
                throw new ArgumentException("Expression is not a valid member expression", nameof(expression));
            }
            PropertyInfo? property = member.Member as PropertyInfo;
            if (property == null)
            {
                return member.Member.Name;
            }
            DisplayAttribute? displayAttribute = property.GetCustomAttributes(typeof(DisplayAttribute), true)
                                               .FirstOrDefault() as DisplayAttribute;
            return displayAttribute?.Name ?? property.Name;
        }
    }
}
