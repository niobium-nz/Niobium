using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Cod.Channel
{
    public static class ModelHelper
    {
        public static object? GetFieldDisplayValue(object? field)
        {
            if (field == null)
            {
                return null;
            }

            Type type = field.GetType();
            if (type.IsEnum)
            {
                MemberInfo? memberInfo = type.GetMember(field.ToString() ?? string.Empty).FirstOrDefault();
                if (memberInfo != null)
                {
                    if (memberInfo.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() is DisplayAttribute displayAttribute)
                    {
                        return displayAttribute.GetName();
                    }
                }
            }

            return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan)
                ? field
                : field.ToString();
        }

        public static IEnumerable<IGrouping<int, DisplayProperty>> GetDisplayProperties(object model)
        {
            Type type = model.GetType();
            return DynamicUICache.DisplayUIProperties.TryGetValue(type, out IGrouping<int, DisplayProperty>[]? cachedProperties) ? cachedProperties : (IEnumerable<IGrouping<int, DisplayProperty>>)[];
        }

        public static IEnumerable<IGrouping<int, EditProperty>> GetEditProperties(object model)
        {
            Type type = model.GetType();
            return DynamicUICache.EditUIProperties.TryGetValue(type, out IGrouping<int, EditProperty>[]? cachedProperties) ? cachedProperties : (IEnumerable<IGrouping<int, EditProperty>>)[];
        }

        public static IEnumerable<IGrouping<int, DisplayAction>> GetDisplayActions(object model)
        {
            Type type = model.GetType();
            return DynamicUICache.UIActions.TryGetValue(type, out IGrouping<int, DisplayAction>[]? cachedActions) ? cachedActions : (IEnumerable<IGrouping<int, DisplayAction>>)[];
        }

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
