using Cod.Contract;

namespace Cod.Platform.Charges
{
    public class Charge
    {
        public ChargeType Type { get; set; }

        public string AppID { get; set; }

        public OpenIDProvider Provider { get; set; }

        public string Order { get; set; }

        public string Account { get; set; }

        public int Amount { get; set; }

        public string Product { get; set; }

        public string IP { get; set; }

        public ChargeResult Result { get; set; }
    }
}
