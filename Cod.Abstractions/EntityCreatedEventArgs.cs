namespace Cod
{
    public class EntityCreatedEventArgs<T> : EventArgs
    {
        public EntityCreatedEventArgs(T entity)
        {
            Entity = entity;
        }

        public T Entity { get; }
    }
}
