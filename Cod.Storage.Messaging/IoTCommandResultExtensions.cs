using Newtonsoft.Json;

namespace Cod.Storage.Messaging
{
    public static class IoTCommandResultExtensions
    {
        public static T GetPayload<T>(this IoTCommandResult result)
        {
            return JsonConvert.DeserializeObject<T>(result.PayloadJSON);
        }
    }
}
