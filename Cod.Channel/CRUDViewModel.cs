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

        protected abstract ICommand CreateCommand { get; }

        protected abstract ICommand UpdateCommand { get; }

        protected abstract ICommand DeleteCommand { get; }

        protected virtual Task SetErrorAsync(string error) => Task.CompletedTask;

        public void RequestCreating()
        {
            this.CreatingValidationState = null;
            this.Creating = new TCreateParameter();
        }

        public void CancelCreating()
        {
            this.Creating = default;
            this.CreatingValidationState = null;
        }

        public void RequestUpdating(TItemViewModel entity)
        {
            this.UpdatingValidationState = null;
            this.Updating = entity;
        }

        public void CancelUpdating()
        {
            this.Updating = default;
            this.UpdatingValidationState = null;
        }

        public async Task CreateAsync()
        {
            if (this.Creating != null)
            {
                var result = await this.CreateCommand.ExecuteAsync(this.Creating);
                if (result.IsSuccess)
                {
                    this.Creating = default;
                    this.CreatingValidationState = null;
                }
                else
                {
                    await SetErrorAsync(result.Message);
                    if (result.Code == InternalError.BadRequest)
                    {
                        this.CreatingValidationState = result.Reference as ValidationState;
                    }
                }
            }
        }

        public async Task UpdateAsync()
        {
            if (this.Updating != null)
            {
                var result = await this.UpdateCommand.ExecuteAsync(this.Updating);
                if (result.IsSuccess)
                {
                    this.Updating = default;
                    this.UpdatingValidationState = null;
                }
                else
                {
                    await SetErrorAsync(result.Message);
                    if (result.Code == InternalError.BadRequest)
                    {
                        this.UpdatingValidationState = result.Reference as ValidationState;
                    }
                }
            }
        }

        public async Task DeleteAsync(TItemViewModel entity)
        {
            if (entity != null)
            {
                await this.DeleteCommand.ExecuteAsync(entity);
            }
        }
    }
}
