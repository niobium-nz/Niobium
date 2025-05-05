namespace Cod
{
    public class EntityChangedEventArgs<T> : EventArgs where T : class
    {
        public EntityChangedEventArgs(T oldEntity, T newEntity)
        {
            OldEntity = oldEntity;
            NewEntity = newEntity;
        }

        public T OldEntity { get; }

        public T NewEntity { get; }
    }
}
