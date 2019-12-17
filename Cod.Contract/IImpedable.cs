namespace Cod
{
    public interface IImpedable
    {
        bool Impeded { get; set; }

        string GetImpedementID();
    }
}
