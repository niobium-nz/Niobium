namespace Cod
{
    [AttributeUsage(AttributeTargets.Property)]
    public class EntityKeyAttribute(EntityKeyKind kind) : Attribute
    {
        public EntityKeyKind Kind { get; set; } = kind;
    }
}
