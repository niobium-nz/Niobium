using Newtonsoft.Json;

namespace Cod.Platform
{
    public static class IoTCommandResultExtensions
    {
        public static T GetPayload<T>(this IoTCommandResult result) => JsonConvert.DeserializeObject<T>(result.PayloadJSON);
    }
}
