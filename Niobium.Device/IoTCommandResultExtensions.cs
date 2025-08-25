using Newtonsoft.Json;

namespace Niobium.Device
{
    public static class IoTCommandResultExtensions
    {
        public static T GetPayload<T>(this IoTCommandResult result)
        {
            return JsonConvert.DeserializeObject<T>(result.PayloadJSON)!;
        }
    }
}
