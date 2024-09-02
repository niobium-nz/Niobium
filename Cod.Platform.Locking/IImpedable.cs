namespace Cod.Platform.Locking
{
    public interface IImpedable
    {
        string GetImpedementID();

        IEnumerable<IImpedimentPolicy> ImpedimentPolicies { get; }
    }
}
