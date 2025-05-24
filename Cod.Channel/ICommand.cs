namespace Cod.Channel
{
    public interface ICommand<TParameter, TResult>
    {
        Task<TResult> ExecuteAsync(TParameter parameter, CancellationToken? cancellationToken = default);
    }

    public interface ICommand<TParameter>
    {
        Task ExecuteAsync(TParameter parameter, CancellationToken? cancellationToken = default);
    }
}
