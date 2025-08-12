using Microsoft.AspNetCore.Components;

namespace Cod.Channel.Blazor.Fluent
{
    public abstract class ViewModelComponentBase<TViewModel> : ComponentBase, IDisposable where TViewModel : IViewModel
    {
        private bool disposed;

        [Parameter]
        public required TViewModel Data { get; set; }

        protected virtual bool IsBusy => Data.IsBusy;

        protected string? ErrorMessage { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Data.RefreshRequested += OnDataRefreshRequested;
            try
            {
                await InitializeViewModelAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred during initialization: {ex.Message}";
            }
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
