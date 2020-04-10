using System;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class CRUDListViewModel<TCreateParameter, TItemViewModel>
        where TCreateParameter : new()
    {
        public ValidationState CreatingValidationState { get; private set; }

        public TCreateParameter Creating { get; private set; }

        public ValidationState UpdatingValidationState { get; private set; }

        public TItemViewModel Updating { get; private set; }

        protected virtual ICommand CreateCommand { get => throw new NotImplementedException(); }

        protected virtual ICommand UpdateCommand { get => throw new NotImplementedException(); }

        protected virtual ICommand DeleteCommand { get => throw new NotImplementedException(); }

        protected virtual Task SetErrorAsync(string error) => Task.CompletedTask;

        protected virtual object ToEntity(TItemViewModel updateParameter) => throw new NotImplementedException();

        public virtual void RequestCreating()
        {
            this.CreatingValidationState = null;
            this.Creating = new TCreateParameter();
        }

        public virtual void CancelCreating()
        {
            this.Creating = default;
            this.CreatingValidationState = null;
        }

        public virtual void RequestUpdating(TItemViewModel entity)
        {
            this.UpdatingValidationState = null;
            this.Updating = entity;
        }

        public virtual void CancelUpdating()
        {
            this.Updating = default;
            this.UpdatingValidationState = null;
        }

        public virtual async Task CreateAsync()
            => await ViewModelHelper.ValidateAndExecuteAsync(
                () => Task.FromResult(this.CreateCommand),
                () => Task.FromResult<object>(this.Creating),
                state =>
                {
                    if (state == null)
                    {
                        this.Creating = default;
                    }
                    this.CreatingValidationState = state;
                    return Task.CompletedTask;
                },
                error => SetErrorAsync(error));

        public virtual async Task UpdateAsync()
            => await ViewModelHelper.ValidateAndExecuteAsync(
                () => Task.FromResult(this.UpdateCommand),
                () => Task.FromResult(this.ToEntity(this.Updating)),
                state =>
                {
                    if (state == null)
                    {
                        this.Updating = default;
                    }
                    this.UpdatingValidationState = state;
                    return Task.CompletedTask;
                },
                error => SetErrorAsync(error));

        public virtual async Task DeleteAsync(StorageKey key)
        {
            await this.DeleteCommand.ExecuteAsync(key);
        }
    }
}
