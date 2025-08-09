namespace Cod.Platform.Locking
{
    public class IImpedimentContext
    {
        public int Cause { get; set; }

        public string? PolicyInput { get; set; }

        public string? Category { get; set; }

        public required string ImpedementID { get; set; }
    }
}