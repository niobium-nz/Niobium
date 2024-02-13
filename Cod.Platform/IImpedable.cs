namespace Cod.Platform
{
    public interface IImpedable
    {
        string GetImpedementID();

        IEnumerable<IImpedimentPolicy> ImpedimentPolicies { get; }
    }
}
