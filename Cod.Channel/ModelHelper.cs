using System.Linq.Expressions;

namespace Cod.Channel
{
    /// <summary>
    /// Facade for display and edit helpers.
    /// </summary>
    public static class ModelHelper
    {
        public static object? GetDisplayValue(object? value)
        {
            return DisplayHelper.GetDisplayValue(value);
        }

        public static IEnumerable<IGrouping<int, DisplayProperty>> GetDisplayProperties(object model)
        {
            return DisplayHelper.GetDisplayProperties(model);
        }

        public static IEnumerable<DisplayAction> GetDisplayActions(object model)
        {
            return DisplayHelper.GetDisplayActions(model);
        }

        public static string GetDisplayName(Type type)
        {
            return DisplayHelper.GetDisplayName(type);
        }

        public static string GetDisplayName<TModel, TProperty>(this TModel model, Expression<Func<TModel, TProperty>> expression)
        {
            return DisplayHelper.GetDisplayName(model, expression);
        }

        public static IEnumerable<IGrouping<int, EditProperty>> GetEditProperties(object model, IEnumerable<IEditModeValueProvider> valueProviders)
        {
            return EditHelper.GetEditProperties(model, valueProviders);
        }
    }
}
