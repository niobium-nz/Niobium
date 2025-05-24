using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Cod.Channel
{
    public static class ModelExtensions
    {
        // Generic method to get display name based on the model and its property
        public static string GetDisplayName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> expression)
        {
            // Get the property info from the lambda expression
            var member = expression.Body as MemberExpression;

            if (member == null)
            {
                // Handle cases where the member expression may be nested in a Convert expression (e.g., boxing)
                var unary = expression.Body as UnaryExpression;
                member = unary?.Operand as MemberExpression;
            }

            if (member == null)
                throw new ArgumentException("Expression is not a valid member expression", nameof(expression));

            // Get the property information for the specified property
            var property = member.Member as PropertyInfo;

            if (property == null) return member.Member.Name; // If property is not found, return the name

            // Get the DisplayAttribute from the property, if available
            var displayAttribute = property.GetCustomAttributes(typeof(DisplayAttribute), true)
                                           .FirstOrDefault() as DisplayAttribute;

            // Return the display name if found, otherwise return the property name
            return displayAttribute?.Name ?? property.Name;
        }
    }
}
