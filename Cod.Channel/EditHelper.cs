using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Cod.Channel
{
    /// <summary>
    /// Provides helper methods for extracting and transforming editable properties from view models.
    /// </summary>
    public static class EditHelper
    {
        /// <summary>
        /// Internal record for holding property metadata and attributes.
        /// </summary>
        private sealed record EditPropertyInfo(DisplayAttribute? Display, EditModeAttribute? Control, PropertyInfo Property);

        /// <summary>
        /// Gets all editable properties for a given model, grouped by their display group.
        /// Uses reflection cache for performance.
        /// </summary>
        /// <param name="model">The view model instance.</param>
        /// <param name="valueProviders">Value providers for custom edit options.</param>
        /// <returns>Grouped editable properties.</returns>
        public static IEnumerable<IGrouping<int, EditProperty>> GetEditProperties(object model, IEnumerable<IEditModeValueProvider> valueProviders)
        {
            Type type = model.GetType();
            PropertyInfo[] properties = ReflectionCacheBootstrapper.PropertyCache.TryGetValue(type, out PropertyInfo[]? cachedProps)
                ? cachedProps
                : type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties
                        .Select(p => new EditPropertyInfo(
                            p.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute,
                            p.GetCustomAttributes(typeof(EditModeAttribute), false).FirstOrDefault() as EditModeAttribute,
                            p))
                        .Where(x => x.Display != null)
                        .Select(x => TransformIntoEditProperty(x, model, valueProviders))
                        .OrderBy(p => p.Order)
                        .GroupBy(p => p.Group);
        }

        /// <summary>
        /// Transforms property metadata and attributes into an EditProperty or its derived type.
        /// </summary>
        private static EditProperty TransformIntoEditProperty(EditPropertyInfo info, object model, IEnumerable<IEditModeValueProvider> valueProviders)
        {
            string? displayName = info.Display?.GetName();
            string propertyName = info.Property.Name;
            Type propertyType = info.Property.PropertyType;
            Func<object, object?> getValue = info.Property.GetValue;
            Action<object, object?> setValue = info.Property.SetValue;
            int? order = info.Display?.GetOrder();
            int group = int.TryParse(info.Display?.GroupName, out int g) ? g : int.MaxValue;
            string? description = info.Display?.GetDescription();
            bool? isReadOnly = info.Control?.IsReadOnly;
            bool? isRequired = info.Control?.IsRequired;
            PropertyControl? control = info.Control?.Control;

            if (info.Control?.Control == PropertyControl.Dropdown)
            {
                IEnumerable<EditOption>? options = null;
                foreach (IEditModeValueProvider provider in valueProviders)
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
                foreach (IEditModeValueProvider provider in valueProviders)
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

        /// <summary>
        /// Gets enum options for dropdowns, including support for bit flag enums.
        /// </summary>
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

            bool isBitFlagEnum = propertyType.GetCustomAttributes(typeof(FlagsAttribute), false).Length > 0;
            List<EditOption> options = [];
            IEnumerable<Enum> enumMembers = Enum.GetValues(propertyType).Cast<Enum>();
            object? actualValue = getValue(model);
            foreach (Enum enumMember in enumMembers)
            {
                MemberInfo memberInfo = propertyType.GetMember(enumMember.ToString()).Single();
                string? memberDisplayName = null;
                if (memberInfo.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() is DisplayAttribute attr)
                {
                    memberDisplayName = attr.GetName();
                }

                memberDisplayName ??= enumMember.ToString();
                int memberValue = Convert.ToInt32(enumMember);
                bool isSelected = false;
                if (actualValue is not null and IConvertible)
                {
                    isSelected = Convert.ToInt32(getValue(model)) == memberValue;
                }

                EditOption option = new(
                    memberDisplayName,
                    memberValue.ToString(),
                    isSelected,
                    isSelected =>
                    {
                        if (isBitFlagEnum)
                        {
                            object? existingValue = getValue(model);
                            if (existingValue is not null and int i)
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
    }
}
