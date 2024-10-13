using CommonServiceLocator;
using Xamarin.Forms;

namespace Cod.Channel.Mobile
{
    public static class ViewModelLocator
    {
        public static readonly BindableProperty AutoWireViewModelProperty =
            BindableProperty.CreateAttached(
                "AutoWireViewModel",
                typeof(bool),
                typeof(ViewModelLocator),
                default(bool),
                propertyChanged: OnAutoWireViewModelChanged);

        public static bool GetAutoWireViewModel(BindableObject bindable)
        {
            ArgumentNullException.ThrowIfNull(bindable);
            return (bool)bindable.GetValue(AutoWireViewModelProperty);
        }

        public static void SetAutoWireViewModel(BindableObject bindable, bool value)
        {
            ArgumentNullException.ThrowIfNull(bindable);
            bindable.SetValue(AutoWireViewModelProperty, value);
        }

        private static void OnAutoWireViewModelChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is not Element view)
            {
                return;
            }

            var viewType = view.GetType();
            var viewName = viewType.FullName.Replace(".Views.", ".ViewModels.");
            var viewAssemblyName = viewType.Assembly.FullName;
            var viewModelName = $"{viewName}Model, {viewAssemblyName}";

            var viewModelType = Type.GetType(viewModelName) ?? throw new NotImplementedException($"ViewModel {viewModelName} does not exist.");
            var viewModel = ServiceLocator.Current.GetInstance(viewModelType);
            view.BindingContext = viewModel;
        }
    }
}
