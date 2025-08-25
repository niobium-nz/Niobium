using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Niobium.Channel
{
    internal sealed class DynamicUICache(IEnumerable<IEditModeValueProvider> valueProviders) : IBootstrapper
    {
        private static readonly List<Assembly> registeredUIAssemblies = [];
        private static readonly ConcurrentDictionary<Type, IGrouping<int, DisplayProperty>[]> displayUIProperties = new();
        private static readonly ConcurrentDictionary<Type, IGrouping<int, EditProperty>[]> editUIProperties = new();
        private static readonly ConcurrentDictionary<Type, IGrouping<int, DisplayAction>[]> uiActions = new();

        public static IReadOnlyDictionary<Type, IGrouping<int, DisplayProperty>[]> DisplayUIProperties => displayUIProperties;
        public static IReadOnlyDictionary<Type, IGrouping<int, EditProperty>[]> EditUIProperties => editUIProperties;
        public static IReadOnlyDictionary<Type, IGrouping<int, DisplayAction>[]> UIActions => uiActions;

        public static void RegisterUIComponents(Type anyTypeFromAssembly)
        {
            if (!registeredUIAssemblies.Contains(anyTypeFromAssembly.Assembly))
            {
                registeredUIAssemblies.Add(anyTypeFromAssembly.Assembly);
            }
        }

        public Task InitializeAsync()
        {
            foreach (Assembly assembly in registeredUIAssemblies)
            {
                IEnumerable<Type> types = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract && typeof(IViewModel).IsAssignableFrom(t));
                foreach (Type type in types)
                {
                    (displayUIProperties[type], editUIProperties[type]) = GetUIPropertyGroups(type);
                    uiActions[type] = GetUIActionGroups(type);
                }
            }

            return Task.CompletedTask;
        }

        private (IGrouping<int, DisplayProperty>[], IGrouping<int, EditProperty>[]) GetUIPropertyGroups(Type type)
        {
            DynamicUISettings[] settings = [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(p => new DynamicUIAttributes(
                    p.GetCustomAttributes(typeof(DisplayAttribute), false).FirstOrDefault() as DisplayAttribute,
                    p.GetCustomAttributes(typeof(DisplayModeAttribute), false).FirstOrDefault() as DisplayModeAttribute,
                    p.GetCustomAttributes(typeof(EditModeAttribute), false).FirstOrDefault() as EditModeAttribute,
                    p))
                .Where(x => x.Display != null)
                .Select(x => new DynamicUISettings(TransformIntoDisplayProperty(x), TransformIntoEditProperty(x)))];

            return ([.. settings.Select(p => p.Display)
                        .OrderBy(p => p.Order)
                        .ToArray()
                        .GroupBy(p => p.Group)],
                    [.. settings.Select(p => p.Edit)
                        .OrderBy(p => p.Order)
                        .ToArray()
                        .GroupBy(p => p.Group)]);
        }

        /// <summary>
        /// Transforms property metadata and attributes into an EditProperty or its derived type.
        /// </summary>
        private EditProperty TransformIntoEditProperty(DynamicUIAttributes attributes)
        {
            string? displayName = attributes.Display?.GetName();
            string propertyName = attributes.Property.Name;
            Type propertyType = attributes.Property.PropertyType;
            Func<object, object?> getValue = attributes.Property.GetValue;
            Action<object, object?> setValue = attributes.Property.SetValue;
            int? order = attributes.Display?.GetOrder();
            int group = int.TryParse(attributes.Display?.GroupName, out int g) ? g : int.MaxValue;
            string? description = attributes.Display?.GetDescription();
            bool? isReadOnly = attributes.EditMode?.IsReadOnly;
            bool? isRequired = attributes.EditMode?.IsRequired;
            PropertyControl? control = attributes.EditMode?.Control;

            if (attributes.EditMode?.Control == PropertyControl.Dropdown)
            {
                IEnumerable<EditOption> getOptions(object instance)
                {
                    foreach (IEditModeValueProvider provider in valueProviders)
                    {
                        object? options = provider.GetValue(instance, attributes.Property.Name, nameof(EditOptionalProperty.GetOptions));
                        if (options is IEnumerable<EditOption> r)
                        {
                            return r;
                        }
                    }

                    List<EditOption>? result = GetEnumOptions(instance, propertyType, getValue, setValue);
                    return result ?? [];
                }

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
                    getOptions: getOptions);
            }
            else if (attributes.EditMode?.Control == PropertyControl.Calendar)
            {
                CalendarKind? getCalendarKind(object instance)
                {
                    foreach (IEditModeValueProvider provider in valueProviders)
                    {
                        object? result = provider.GetValue(instance, attributes.Property.Name, nameof(EditDateTimeProperty.GetCalendarKind));
                        if (result is CalendarKind r)
                        {
                            return r;
                        }
                    }

                    return attributes.EditMode?.Calendar;
                }


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
                    getCalendarKind: getCalendarKind);
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

        private static IGrouping<int, DisplayAction>[] GetUIActionGroups(Type type)
        {
            return [.. type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
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
                    order: x.Action?.GetOrder(),
                    group: int.TryParse(x.Action?.GroupName, out int g) ? g : int.MaxValue,
                    description: x.Action?.GetDescription()))
                .OrderBy(p => p.Order)
                .ToArray()
                .GroupBy(p => p.Group)];
        }

        private static DisplayProperty TransformIntoDisplayProperty(DynamicUIAttributes attributes)
        {
            return new(attributes.Property.Name,
                            attributes.Display?.GetName(),
                            attributes.Property.GetValue,
                            order: attributes.Display?.GetOrder(),
                            group: int.TryParse(attributes.Display?.GroupName, out int g) ? g : int.MaxValue,
                            description: attributes.Display?.GetDescription(),
                            isSubject: attributes.DisplayMode?.IsSubject,
                            control: attributes.DisplayMode?.Control);
        }

        /// <summary>
        /// Gets enum options for dropdowns, including support for bit flag enums.
        /// </summary>
        private static List<EditOption>? GetEnumOptions(
            object model,
            Type propertyType,
            Func<object, object?> getValue,
            Action<object, object> setValue)
        {
            if (!propertyType.IsEnum)
            {
                return null;
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

        private sealed record DynamicUIAttributes(DisplayAttribute? Display, DisplayModeAttribute? DisplayMode, EditModeAttribute? EditMode, PropertyInfo Property);

        private sealed record DynamicUISettings(DisplayProperty Display, EditProperty Edit);
    }
}
