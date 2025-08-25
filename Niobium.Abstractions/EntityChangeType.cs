namespace Niobium
{
    [Flags]
    public enum EntityChangeType : int
    {
        /// <summary>
        /// The entity has not been changed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The entity has been created.
        /// </summary>
        Created = 1,

        /// <summary>
        /// The entity has been updated.
        /// </summary>
        Updated = 2,

        /// <summary>
        /// The entity has been deleted.
        /// </summary>
        Deleted = 4,
    }
}
