using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor.Fluent
{
    public abstract class ViewModelComponentBase<TViewModel> : ComponentBase, IDisposable where TViewModel : IViewModel
    {
        private bool disposed;

        [Parameter]
        public required TViewModel Data { get; set; }

        protected virtual bool IsBusy => Data.IsBusy;

        protected override async Task OnInitializedAsync()
        {
            Data.RefreshRequested += OnDataRefreshRequested;
            await InitializeViewModelAsync();
            await base.OnInitializedAsync();
        }

        protected virtual Task InitializeViewModelAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnDataRefreshRequested(object? sender, EventArgs e)
        {
            InvokeAsync(StateHasChanged);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Data.RefreshRequested -= OnDataRefreshRequested;
                disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
