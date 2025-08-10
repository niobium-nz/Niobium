namespace Cod.Channel
{
    public abstract class BaseViewModel : IViewModel
    {
        public IRefreshable? Parent { get; private set; }

        public virtual bool IsInitialized { get; protected set; }

        public abstract bool IsBusy { get; }

        public EventHandler? RefreshRequested { get; set; }

        public async virtual Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            if (Parent != null)
            {
                await Parent.RefreshAsync(cancellationToken);
            }
            else
            {
                OnRefreshRequested();
            }
        }

        protected Task InitializeAsync(IRefreshable? parent = null, CancellationToken cancellationToken = default)
        {
            Parent = parent;
            IsInitialized = true;
            return Task.CompletedTask;
        }

        protected virtual void OnRefreshRequested() => RefreshRequested?.Invoke(this, EventArgs.Empty);
    }
}
