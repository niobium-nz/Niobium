using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cod.Channel
{
    public abstract class CRUDListViewModel<T> : CRUDListViewModel<T, T> where T : new() { }

    public abstract class CRUDListViewModel<TCreateParameter, TUpdateParameter>
        where TCreateParameter : new()
    {
        public TCreateParameter Creating { get; protected set; }

        public TUpdateParameter Updating { get; protected set; }

        protected virtual ICommand<TCreateParameter> CreateCommand => throw new NotImplementedException();

        protected virtual ICommand<TUpdateParameter> UpdateCommand => throw new NotImplementedException();

        protected virtual ICommand<StorageKey> DeleteCommand => throw new NotImplementedException();

        protected virtual Task OnCreateError(CommandExecutionEventArgs args) => Task.CompletedTask;

        protected virtual Task OnCreateSuccess(CommandExecutionEventArgs args)
        {
            this.Creating = default;
            return Task.CompletedTask;
        }

        protected virtual Task OnUpdateError(CommandExecutionEventArgs args) => Task.CompletedTask;

        protected virtual Task OnModelStateError(IReadOnlyDictionary<string, IEnumerable<string>> errors) => Task.CompletedTask;

        protected virtual Task OnUpdateSuccess(CommandExecutionEventArgs args)
        {
            this.Updating = default;
            return Task.CompletedTask;
        }

        public virtual void RequestCreating(object parameter = null) => this.Creating = new TCreateParameter();

        public virtual void CancelCreating() => this.Creating = default;

        public virtual void RequestUpdating(object parameter = null)
        {
            if (parameter != null && parameter is TUpdateParameter p)
            {
                this.Updating = p;
            }
        }

        public virtual void CancelUpdating() => this.Updating = default;

        public virtual async Task CreateAsync()
        {
            var result = await this.CreateCommand.ExecuteAsync(this.Creating);
            if (result.Result.IsSuccess)
            {
                await this.OnCreateSuccess(result);
            }
            else if (result.Result.Code == InternalError.BadRequest)
            {
                await this.OnModelStateError((IReadOnlyDictionary<string, IEnumerable<string>>)result.Result.Reference);
            }
            else
            {
                await this.OnCreateError(result);
            }
        }

        public virtual async Task UpdateAsync()
        {
            var result = await this.UpdateCommand.ExecuteAsync(this.Updating);
            if (result.Result.IsSuccess)
            {
                await this.OnUpdateSuccess(result);
            }
            else if (result.Result.Code == InternalError.BadRequest)
            {
                await this.OnModelStateError((IReadOnlyDictionary<string, IEnumerable<string>>)result.Result.Reference);
            }
            else
            {
                await this.OnUpdateError(result);
            }
        }

        public virtual async Task DeleteAsync(StorageKey key) => await this.DeleteCommand.ExecuteAsync(key);
    }
}
