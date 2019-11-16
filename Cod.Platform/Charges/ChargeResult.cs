using System.Collections.Generic;

namespace Cod.Platform.Charges
{
    public class ChargeResult
    {
        public Dictionary<string, object> Params { get; set; }

        public ChargeResult(Dictionary<string, object> resultParam)
        {
            this.Params = resultParam;
        }
    }
}
