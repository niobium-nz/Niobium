using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using System.Reflection;

namespace Cod.Channel
{
    public static class ModelHelper
    {
        public static object? GetDisplayValue(object? value)
        {
            if (value == null)
            {
                return null;
            }

            var type = value.GetType();
            if (type.IsEnum)
            {
                var memberInfo = type.GetMember(value.ToString() ?? string.Empty).FirstOrDefault();
                if (memberInfo != null)
                {
                    if (memberInfo.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() is DisplayAttribute displayAttribute)
                    {
                        return displayAttribute.GetName();
                    }
                }
            }

            if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(TimeSpan))
            {
                return value;
            }

            return value.ToString();
        }

        public static IEnumerable<IGrouping<int, DisplayProperty>> GetDisplayProperies(object model)
        {
            var type = model.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
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
                    group: int.TryParse(x.Display?.GroupName, out var g) ? g : int.MaxValue,
                    description: x.Display?.GetDescription(),
                    isSubject: x.Control?.IsSubject,
                    control: x.Control?.Control))
                .OrderBy(p => p.Order)
                .GroupBy(p => p.Group);
        }

        private static EditProperty TransformIntoEditProperty(EditPropertyInfo info, object model, IEnumerable<IEditModeValueProvider> valueProviders)
        {
            var displayName = info.Display?.GetName();
            var propertyName = info.Property.Name;
            var propertyType = info.Property.PropertyType;
            Func<object, object?> getValue = info.Property.GetValue;
            Action<object, object?> setValue = info.Property.SetValue;
            var order = info.Display?.GetOrder();
            var group = int.TryParse(info.Display?.GroupName, out var g) ? g : int.MaxValue;
            var description = info.Display?.GetDescription();
            var isReadOnly = info.Control?.IsReadOnly;
            var isRequired = info.Control?.IsRequired;
            var control = info.Control?.Control;

            if (info.Control?.Control == PropertyControl.Dropdown)
            {
                IEnumerable<EditOption>? options = null;
                foreach (var provider in valueProviders)
                {
                    options = provider.GetValue(model, info.Property.Name, nameof(EditOptionalProperty.Options)) as IEnumerable<EditOption>;
                    if (options != null)
                    {
                        break;
                    }
                }
                options ??= GetEnumOptions(model, propertyType, getValue, setValue);

                return new EditOptionalProperty(
                    displayName: displayName,
                    propertyName: propertyName,
                    propertyType: propertyType,
                    getValue: getValue,
                    setValue: setValue,
                    order: order,
                    group: group,
                    description: description,
                    control: control,
                    isReadOnly: isReadOnly,
                    isRequired: isRequired,
                    options: options);
            }
            else if (info.Control?.Control == PropertyControl.Calendar)
            {
                CalendarKind? kind = null;
                foreach (var provider in valueProviders)
                {
                    kind = provider.GetValue(model, info.Property.Name, nameof(EditDateTimeProperty.Calendar)) as CalendarKind?;
                    if (kind.HasValue)
                    {
                        break;
                    }
                }
                kind ??= info.Control?.Calendar;

                return new EditDateTimeProperty(
                    displayName: displayName,
                    propertyName: propertyName,
                    propertyType: propertyType,
                    getValue: getValue,
                    setValue: setValue,
                    order: order,
                    group: group,
                    description: description,
                    control: control,
                    isReadOnly: isReadOnly,
                    isRequired: isRequired,
                    calendar: kind);
            }
            else
            {
                return new EditProperty(
                    displayName: displayName,
                    propertyName: propertyName,
                    propertyType: propertyType,
                    getValue: getValue,
                    setValue: setValue,
                    order: order,
                    group: group,
                    description: description,
                    control: control,
                    isReadOnly: isReadOnly,
                    isRequired: isRequired);
            }
        }

        private static List<EditOption> GetEnumOptions(
            object model,
            Type propertyType,
            Func<object, object?> getValue,
            Action<object, object> setValue)
        {
            if (!propertyType.IsEnum)
            {
                return [];
            }

            var isBitFlagEnum = propertyType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
            var options = new List<EditOption>();
            var enumMembers = Enum.GetValues(propertyType).Cast<Enum>();
            var actualValue = getValue(model);
            foreach (var enumMember in enumMembers)
            {
                var memberInfo = propertyType.GetMember(enumMember.ToString()).Single();
                string? memberDisplayName = null;
                if (memberInfo.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() is DisplayAttribute attr)
                {
                    memberDisplayName = attr.GetName();
                }

                memberDisplayName ??= enumMember.ToString();
                var memberValue = Convert.ToInt32(enumMember);
                var isSelected = false;
                if (actualValue != null && actualValue is IConvertible)
                {
                    isSelected = Convert.ToInt32(getValue(model)) == memberValue;
                }

                var option = new EditOption(
                    memberDisplayName,
                    memberValue.ToString(),
                    isSelected,
                    isSelected =>
                    {
                        if (isBitFlagEnum)
                        {
                            var existingValue = getValue(model);
                            if (existingValue != null && existingValue is int i)
                            {
                                if (isSelected)
                                {
                                    setValue(model, memberValue | i);
                                }
                                else
                                {
                                    setValue(model, memberValue ^ i);
                                }

                                return;
                            }
                        }

                        if (isSelected)
                        {
                            setValue(model, memberValue);
                        }
                    });
                options.Add(option);
            }

            return options;
        }

        public static IEnumerable<IGrouping<int, EditProperty>> GetEditProperies(object model, IEnumerable<IEditModeValueProvider> valueProviders)
          => model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new EditPropertyInfo(
                    p.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute,
                    p.GetCustomAttributes(typeof(EditModeAttribute), false).FirstOrDefault() as EditModeAttribute,
                    p))
                .Where(x => x.Display != null)
                .Select(x => TransformIntoEditProperty(x, model, valueProviders))
                .OrderBy(p => p.Order)
                .GroupBy(p => p.Group);

        public static IEnumerable<DisplayAction> GetDisplayActions(object model)
        {
            var type = model.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
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
                        var r = x.Method.Invoke(i, null);
                        if (r is Task task)
                        {
                            return task;
                        }
                        return Task.CompletedTask;
                    },
                    description: x.Action?.GetDescription(),
                    order: x.Action?.GetOrder()))
                .OrderBy(p => p.Order);
        }

        public static string GetDisplayName(Type type)
        {
            var attr = type.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute;
            return attr?.GetName() ?? type.Name;
        }

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

        private record EditPropertyInfo(DisplayAttribute? Display, EditModeAttribute? Control, PropertyInfo Property);
    }
}
