using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class CRUDListViewModel<TCreateParameter, TUpdateParameter>
        where TCreateParameter : new()
    {
        public TCreateParameter Creating { get; private set; }

        public TUpdateParameter Updating { get; private set; }

        protected virtual ICommand CreateCommand { get => throw new NotImplementedException(); }

        protected virtual ICommand UpdateCommand { get => throw new NotImplementedException(); }

        protected virtual ICommand DeleteCommand { get => throw new NotImplementedException(); }

        protected virtual Task OnCreateError(CommandExecutionEventArgs args) => Task.CompletedTask;

        protected virtual Task OnCreateSuccess(CommandExecutionEventArgs args)
        {
            this.Creating = default;
            return Task.CompletedTask;
        }

        protected virtual Task OnUpdateError(CommandExecutionEventArgs args) => Task.CompletedTask;

        protected virtual Task OnUpdateSuccess(CommandExecutionEventArgs args)
        {
            this.Updating = default;
            return Task.CompletedTask;
        }

        protected virtual object BuildUpdateParameter() => throw new NotImplementedException();

        public virtual void RequestCreating(object parameter) => this.Creating = new TCreateParameter();

        public virtual void CancelCreating() => this.Creating = default;

        public virtual void RequestUpdating(TUpdateParameter obj, object parameter) => this.Updating = obj;

        public virtual void CancelUpdating() => this.Updating = default;

        public virtual async Task CreateAsync()
            => await ViewModelHelper.ValidateAndExecuteAsync(
                () => Task.FromResult(this.CreateCommand),
                () => Task.FromResult<object>(this.Creating),
                this.OnCreateSuccess,
                this.OnCreateError);

        public virtual async Task UpdateAsync()
            => await ViewModelHelper.ValidateAndExecuteAsync(
                () => Task.FromResult(this.UpdateCommand),
                () => Task.FromResult(this.BuildUpdateParameter()),
                this.OnUpdateSuccess,
                this.OnUpdateError);

        public virtual async Task DeleteAsync(StorageKey key) => await this.DeleteCommand.ExecuteAsync(key);
    }
}
