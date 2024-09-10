namespace System.Runtime.CompilerServices 
{
    [AttributeUsage(AttributeTargets.All)]
    internal class RequiredMemberAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.All)]
    internal class CompilerFeatureRequiredAttribute : Attribute
    {
        public CompilerFeatureRequiredAttribute(string name) { }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
    internal sealed class SetsRequiredMembersAttribute : Attribute
    {
    }
}

