namespace Cod.Contract
{
    public interface IImpedable
    {
        bool Impeded { get; set; }

        string GetImpedementID();
    }
}
