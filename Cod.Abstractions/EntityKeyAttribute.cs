namespace Cod
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityKeyAttribute : Attribute
    {
        public EntityKeyKind Kind { get; set; }

        public EntityKeyAttribute(EntityKeyKind kind)
        {
            Kind = kind;
        }
    }
}
